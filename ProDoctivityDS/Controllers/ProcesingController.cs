using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para el procesamiento de documentos (inicio, progreso, cancelación)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessingController : ControllerBase
    {
        private readonly IProcessingProgressStore _progressStore;
        private readonly ILogger<ProcessingController> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISelectionService _selectionService;
        private readonly IDocumentDeletionService _deletionService;

        // Almacenamiento en memoria de los cancellation tokens por sesión
        private static readonly Dictionary<string, CancellationTokenSource> _activeProcesses = new();

        public ProcessingController(
            IProcessingProgressStore progressStore,
            ILogger<ProcessingController> logger,
            IServiceScopeFactory scopeFactory,
            ISelectionService selectionService,
            IDocumentDeletionService deletionService)
        {
            _progressStore = progressStore;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _selectionService = selectionService;
            _deletionService = deletionService;
        }

        private string GetOrCreateSessionId()
        {
            if (Request.Headers.TryGetValue("X-Session-Id", out var sessionId))
            {
                _logger.LogInformation("Header X-Session-Id recibido: {SessionId}", sessionId);
                return sessionId.ToString();
            }

            var newSessionId = Guid.NewGuid().ToString();
            Response.Headers.Add("X-Session-Id", newSessionId);
            _logger.LogInformation("Nuevo X-Session-Id generado: {SessionId}", newSessionId);
            return newSessionId;
        }

        /// <summary>
        /// Inicia el procesamiento de los documentos seleccionados.
        /// El progreso puede consultarse mediante GET /progress.
        /// </summary>
        /// <param name="request">Lista de IDs de documentos a procesar y opciones específicas</param>
        /// <returns>Información de la sesión iniciada</returns>
        /// <response code="202">Procesamiento aceptado e iniciado</response>
        /// <response code="400">Solicitud inválida (lista vacía, etc.)</response>
        /// <response code="500">Error interno al iniciar</response>
        [HttpPost("start")]
        [ProducesResponseType(typeof(object), 202)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> StartProcessing([FromBody] ProcessRequestDto request)
        {
            var sessionId = GetOrCreateSessionId();
            _logger.LogInformation(">>> INICIO StartProcessing para sessionId={SessionId}", sessionId);

            // Validar si ya hay un proceso activo
            if (_activeProcesses.ContainsKey(sessionId))
            {
                _logger.LogWarning(">>> Proceso ya activo para {SessionId}, rechazando", sessionId);
                return Conflict(new { message = "Ya hay un proceso en curso" });
            }

            // Obtener documentos seleccionados
            var selectedEnumerable = await _selectionService.GetSelectedDocumentsAsync(sessionId); 
            var selectedIds = selectedEnumerable.ToList();
            _logger.LogInformation(">>> Documentos seleccionados: {Count} para sesión {SessionId}", selectedIds.Count, sessionId);
            if (selectedIds.Count == 0)
            {
                _logger.LogWarning(">>> No hay documentos seleccionados para sesión {SessionId}", sessionId);
                return BadRequest(new { message = "No hay documentos seleccionados" });
            }

            // Asignar IDs a procesar
            if (request.DocumentIds == null || !request.DocumentIds.Any())
            {
                request.DocumentIds = selectedIds;
                _logger.LogInformation(">>> Usando documentos seleccionados: {Ids}", string.Join(",", selectedIds));
            }
            else
            {
                request.DocumentIds = request.DocumentIds.Intersect(selectedIds).ToList();
                _logger.LogInformation(">>> Usando documentos enviados (filtrados): {Ids}", string.Join(",", request.DocumentIds));
            }

            if (!request.DocumentIds.Any())
            {
                _logger.LogWarning(">>> Lista de documentos vacía después de filtrar");
                return BadRequest(new { message = "No hay documentos válidos para procesar" });
            }

            // Crear cancellation token
            var cts = new CancellationTokenSource();
            _activeProcesses[sessionId] = cts;
            _logger.LogInformation(">>> CancellationToken creado para sesión {SessionId}", sessionId);

            // Progreso inicial
            var initialProgress = new ProcessProgressDto
            {
                Total = request.DocumentIds.Count,
                Processed = 0,
                Updated = 0,
                PagesRemoved = 0,
                Errors = 0,
                Skipped = 0,
                Status = "Iniciando",
                CurrentDocumentName = null,
                CurrentDocumentId = null
            };
            _logger.LogInformation(">>> ANTES de UpdateProgress para sesión {SessionId}", sessionId);
            _progressStore.UpdateProgress(sessionId, initialProgress);
            _logger.LogInformation(">>> DESPUÉS de UpdateProgress para sesión {SessionId}", sessionId);

            // Lanzar tarea en segundo plano
            _ = Task.Run(async () =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var processingService = scope.ServiceProvider.GetRequiredService<IProcessingService>();
                    try
                    {
                        _logger.LogInformation(">>> Hilo de procesamiento iniciado para sesión {SessionId}", sessionId);
                        await processingService.ProcessDocumentsAsync(request, sessionId, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation(">>> Procesamiento cancelado para sesión {SessionId}", sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ">>> Error en procesamiento para sesión {SessionId}", sessionId);
                        _progressStore.UpdateProgress(sessionId, new ProcessProgressDto
                        {
                            Total = request.DocumentIds.Count,
                            Status = $"Error crítico: {ex.Message}",
                            Errors = request.DocumentIds.Count
                        });
                    }
                    finally
                    {
                        _activeProcesses.Remove(sessionId);
                        _logger.LogInformation(">>> Procesamiento finalizado para sesión {SessionId}", sessionId);
                    }
                }
            }, CancellationToken.None);

            _logger.LogInformation(">>> Respuesta Accepted para sesión {SessionId}", sessionId);
            return Accepted(new { sessionId, message = "Procesamiento iniciado" });
        }


        /// <summary>
        /// Obtiene el progreso actual del procesamiento para la sesión actual.
        /// </summary>
        /// <returns>Estado del progreso</returns>
        /// <response code="200">Progreso obtenido (puede estar completo o en curso)</response>
        /// <response code="404">No hay proceso activo para esta sesión</response>
        [HttpGet("progress")]
        [ProducesResponseType(typeof(ProcessProgressDto), 200)]
        [ProducesResponseType(404)]
        public ActionResult<ProcessProgressDto> GetProgress([FromHeader(Name = "X-Session-Id")] string sessionId)
        {
            var SessionId = GetOrCreateSessionId();
            Response.Headers.TryAdd("X-Session-Id", SessionId);
            _logger.LogInformation("StartProcessing: sessionId={SessionId}", sessionId);
            var progress = _progressStore.GetProgress(SessionId);

            if (progress == null)
                return NotFound(new { message = "No hay proceso activo para esta sesión" });

            return Ok(progress);
        }

        /// <summary>
        /// Cancela el procesamiento en curso para la sesión actual.
        /// </summary>
        /// <returns>Resultado de la cancelación</returns>
        /// <response code="200">Cancelación solicitada correctamente</response>
        /// <response code="404">No hay proceso activo para cancelar</response>
        [HttpPost("cancel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult CancelProcessing()
        {
            var SessionId = GetOrCreateSessionId();
            Response.Headers.TryAdd("X-Session-Id", SessionId);
            if (_activeProcesses.TryGetValue(SessionId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Cancelación solicitada para sesión {SessionId}", SessionId);

                // Actualizar progreso para reflejar cancelación
                var progress = _progressStore.GetProgress(SessionId);
                if (progress != null)
                {
                    progress.Status = "Cancelado por usuario";
                    _progressStore.UpdateProgress(SessionId, progress);
                }

                return Ok(new { message = "Cancelación solicitada" });
            }

            return NotFound(new { message = "No hay proceso activo para esta sesión" });
        }

        /// <summary>
        /// Elimina el progreso almacenado para la sesión actual (útil cuando el frontend cierra la vista).
        /// </summary>
        /// <returns>Resultado de la operación</returns>
        /// <response code="200">Progreso eliminado</response>
        [HttpDelete("progress")]
        [ProducesResponseType(200)]
        public IActionResult ClearProgress()
        {
            var SessionId = GetOrCreateSessionId();
            Response.Headers.TryAdd("X-Session-Id", SessionId);
            _progressStore.RemoveProgress(SessionId);
            _logger.LogDebug("Progreso eliminado para sesión {SessionId}", SessionId);
            return Ok(new { message = "Progreso eliminado" });
        }

        /// <summary>
        /// Elimina un documento por su ID.
        /// </summary>
        /// <param name="documentId">ID del documento a eliminar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteDocument(string documentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest(new { message = "El ID del documento es requerido." });

            try
            {
                var deleted = await _deletionService.DeleteDocumentAsync(documentId, cancellationToken);
                if (deleted)
                    return Ok(new { message = "Documento eliminado correctamente." });
                else
                    return NotFound(new { message = "Documento no encontrado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar documento {DocumentId}", documentId);
                return StatusCode(500, new { message = "Error interno al eliminar el documento." });
            }
        }
    }
}
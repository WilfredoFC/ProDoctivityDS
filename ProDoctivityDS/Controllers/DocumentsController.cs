using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    /// <summary>
    /// Controlador para gestión de documentos (búsqueda y detalles)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(ISearchService searchService, ILogger<DocumentsController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Buscar todos los documentos aplicando filtros opcionales (tipo, nombre, paginación)
        /// </summary>
        /// <param name="page">Número de página (por defecto 0)</param>
        /// <param name="rowsPerPage">Filas por página (por defecto 100, máximo 500)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista paginada de documentos</returns>
        /// <response code="200">Búsqueda exitosa, devuelve documentos</response>
        /// <response code="400">Parámetros inválidos</response>
        /// <response code="401">No autenticado o credenciales incorrectas</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(SearchDocumentsResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SearchDocumentsResponseDto>> SearchAllDocuments(
            [FromQuery] int page = 0,
            [FromQuery] int rowsPerPage = 100,
            CancellationToken cancellationToken = default)
        {
            // Validar parámetros básicos
            if (page < 0)
                return BadRequest(new { message = "El número de página no puede ser negativo" });

            if (rowsPerPage < 1 || rowsPerPage > 500)
                return BadRequest(new { message = "rowsPerPage debe estar entre 1 y 500" });

            try
            { 

                var request = new SearchDocumentsRequestDto
                {
                    Page = page,
                    RowsPerPage = rowsPerPage
                };

                var result = await _searchService.SearchAllAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Error de autenticación al buscar documentos");
                return Unauthorized(new { message = "Credenciales API inválidas o expiradas" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar documentos");
                return StatusCode(500, new { message = "Error interno al buscar documentos" });
            }
        }

        /// <summary>
        /// Busca documentos aplicando filtros opcionales (tipo, nombre, paginación)
        /// </summary>
        /// <param name="documentTypeIds">Lista de IDs de tipos de documento (separados por comas) opcional</param>
        /// <param name="query">Filtro por nombre (opcional)</param>
        /// <param name="page">Número de página (por defecto 0)</param>
        /// <param name="rowsPerPage">Filas por página (por defecto 100, máximo 500)</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Lista paginada de documentos</returns>
        /// <response code="200">Búsqueda exitosa, devuelve documentos</response>
        /// <response code="400">Parámetros inválidos</response>
        /// <response code="401">No autenticado o credenciales incorrectas</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpPost]
        [ProducesResponseType(typeof(SearchDocumentsPOSTResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SearchDocumentsPOSTResponseDto>> SearchDocuments(
            [FromQuery] string? documentTypeIds,  // nombre exacto
            [FromQuery] string? query,
            [FromQuery] int page = 0,
            [FromQuery] int rowsPerPage = 100,
            CancellationToken cancellationToken = default)
        {
            // Validar parámetros básicos
            if (page < 0)
                return BadRequest(new { message = "El número de página no puede ser negativo" });

            if (rowsPerPage < 1 || rowsPerPage > 500)
                return BadRequest(new { message = "rowsPerPage debe estar entre 1 y 500" });

            try
            {
                // Convertir documentTypeIds de string separado por comas a lista
                List<string>? typeIdsList = null;
                if (!string.IsNullOrWhiteSpace(documentTypeIds))
                {
                    // Split by comma, trim each entry, remove empty entries
                    typeIdsList = documentTypeIds
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                var request = new SearchDocumentsPOSTRequestDto
                {
                    DocumentTypeId = typeIdsList,
                    Name = query,
                    Page = page,
                    RowsPerPage = rowsPerPage
                };

                var result = await _searchService.SearchAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Error de autenticación al buscar documentos");
                return Unauthorized(new { message = "Credenciales API inválidas o expiradas" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar documentos");
                return StatusCode(500, new { message = "Error interno al buscar documentos" });
            }
        }

        /// <summary>
        /// Obtiene un documento específico por su ID
        /// </summary>
        /// <param name="id">ID del documento</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Detalles del documento</returns>
        /// <response code="200">Documento encontrado</response>
        /// <response code="400">ID inválido</response>
        /// <response code="401">No autenticado</response>
        /// <response code="404">Documento no encontrado</response>
        /// <response code="500">Error interno del servidor</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DocumentDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DocumentDto>> GetDocument(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "El ID del documento es requerido" });

            try
            {
                var document = await _searchService.GetDocumentAsync(id, cancellationToken);
                if (document == null)
                    return NotFound(new { message = $"Documento con ID {id} no encontrado" });

                return Ok(document);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Error de autenticación al obtener documento {DocumentId}", id);
                return Unauthorized(new { message = "Credenciales API inválidas o expiradas" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation(ex, "Documento no encontrado: {DocumentId}", id);
                return NotFound(new { message = ex.Message ?? $"Documento con ID {id} no encontrado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documento {DocumentId}", id);
                return StatusCode(500, new { message = "Error interno al obtener documento" });
            }
        }

        [HttpGet("{documentId}/identity-number")]
        public async Task<ActionResult<DocumentIdentityNumberResponse>> GetIdentityNumber(string documentId, CancellationToken cancellationToken)
        {
            try
            {
                var number = await _searchService.GetDocumentIdentityNumberAsync(documentId, cancellationToken);
                return Ok(new DocumentIdentityNumberResponse { DocumentId = documentId, IdentityNumber = number });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener identity number para documento {DocumentId}", documentId);
                return StatusCode(500, new { message = "Error interno al obtener el número de identidad" });
            }
        }

        [HttpGet("{documentId}/matricula")]
        public async Task<ActionResult<DocumentIdentityNumberResponse>> GetMatricula(string documentId, CancellationToken cancellationToken)
        {
            try
            {
                var number = await _searchService.GetMatriculaNumberAsync(documentId, cancellationToken);
                return Ok(new DocumentIdentityNumberResponse { DocumentId = documentId, IdentityNumber = number });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener identity number para documento {DocumentId}", documentId);
                return StatusCode(500, new { message = "Error interno al obtener el número de identidad" });
            }
        }
    }
}
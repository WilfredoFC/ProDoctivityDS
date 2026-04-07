using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly IPdfAnalyzer _pdfAnalyzer;
        private readonly IPdfManipulator _pdfManipulator;
        private readonly IFileStorageService _fileStorage;
        private readonly IProcessingProgressStore _progressStore;
        private readonly ILogger<ProcessingService> _logger;

        public ProcessingService(
            IStoredConfigurationRepository configurationRepository,
            IProductivityApiClient apiClient,
            IPdfAnalyzer pdfAnalyzer,
            IPdfManipulator pdfManipulator,
            IFileStorageService fileStorage,
            IProcessingProgressStore progressStore,
            ILogger<ProcessingService> logger)
        {
            _configurationRepository = configurationRepository;
            _apiClient = apiClient;
            _pdfAnalyzer = pdfAnalyzer;
            _pdfManipulator = pdfManipulator;
            _fileStorage = fileStorage;
            _progressStore = progressStore;
            _logger = logger;
        }

        public async Task ProcessDocumentsAsync(
            ProcessRequestDto request,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            // 1. Obtener configuración activa
            StoredConfiguration config = await _configurationRepository.GetActiveConfigurationAsync();
            ProcessingOptions processingOptions = config.ProcessingOptions ?? new ProcessingOptions();
            AnalysisRuleSet analysisRules = config.AnalysisRules ?? new AnalysisRuleSet();

            // Sobrescribir opciones con las de la request si vienen
            bool updateApi = request.UpdateApi ?? processingOptions.UpdateApi;
            bool saveOriginals = request.SaveOriginals ?? processingOptions.SaveOriginalFiles;

            int total = request.DocumentIds.Count;
            int processed = 0, updated = 0, pagesRemoved = 0, errors = 0, skipped = 0;

            // Inicializar progreso
            ProcessProgressDto initialProgress = new ProcessProgressDto
            {
                Total = total,
                Processed = 0,
                Updated = 0,
                PagesRemoved = 0,
                Errors = 0,
                Skipped = 0,
                CurrentDocumentName = "Iniciando...",
                Status = "Iniciando"
            };
            _logger.LogDebug("Actualizando progreso en servicio para sesión {SessionId}: {Processed}/{Total}", sessionId, processed, total);
            _progressStore.UpdateProgress(sessionId, initialProgress);

            await LogAsync("INFO", "Procesamiento",
                $"Iniciando procesamiento de {total} documento(s) (sesión: {sessionId})");

            for (int i = 0; i < request.DocumentIds.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await LogAsync("WARNING", "Procesamiento",
                        $"Procesamiento cancelado por el usuario (sesión: {sessionId})");
                    break;
                }

                var documentId = request.DocumentIds[i];
                string documentName = string.Empty;

                try
                {
                    // 2.1 Obtener información del documento
                    _logger.LogDebug("Obteniendo información del documento {DocumentId}", documentId);
                    ProductivityDocumentDto document = await _apiClient.GetDocumentAsync(
                        config.ApiBaseUrl,
                        config.BearerToken,
                        documentId,
                        cancellationToken);

                    documentName = document.Name;

                    // Actualizar progreso
                    ProcessProgressDto currentProgress = new ProcessProgressDto
                    {
                        Total = total,
                        Processed = processed,
                        Updated = updated,
                        PagesRemoved = pagesRemoved,
                        Errors = errors,
                        Skipped = skipped,
                        CurrentDocumentId = documentId,
                        CurrentDocumentName = documentName,
                        Status = $"Procesando {i + 1}/{total}: {documentName}"
                    };
                    _progressStore.UpdateProgress(sessionId, currentProgress);

                    // 2.2 Obtener la última versión del documento
                    string versionId = document.LastDocumentVersionId ?? "";
                    if (string.IsNullOrEmpty(versionId))
                    {
                        _logger.LogInformation("Documento {DocumentId} sin versión directa, consultando versiones", documentId);
                        var versions = await _apiClient.GetDocumentVersionsAsync(
                            config.ApiBaseUrl,
                            config.BearerToken,
                            documentId,
                            cancellationToken);

                        var lastVersion = versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                        versionId = lastVersion?.DocumentVersionId ?? "";

                        if (string.IsNullOrEmpty(versionId))
                        {
                            throw new Exception("No se pudo obtener ninguna versión del documento");
                        }
                    }

                    // 2.3 Obtener detalle completo de la versión (incluye binarios y metadatos)
                    var versionDetail = await _apiClient.GetDocumentVersionDetailAsync(
                        config.ApiBaseUrl,
                        config.BearerToken,
                        documentId,
                        versionId,
                        cancellationToken);

                    // Extraer el primer data URL que sea PDF
                    var pdfDataUrl = versionDetail?.Document?.Binaries?
                        .FirstOrDefault(b => b.Contains("application/pdf") || b.Contains("application/octet-stream"));

                    if (string.IsNullOrEmpty(pdfDataUrl))
                        throw new Exception("No se encontró PDF en los binarios de la versión");

                    byte[] pdfBytes = DataUrlToBytes(pdfDataUrl); // Necesitas este método auxiliar

                    // Guardar los metadatos originales para usarlos en la subida
                    var originalData = versionDetail.Document.Data;
                    var originalFilesName = versionDetail.Document.FilesName;

                    // Guardar original (si está activado) - igual que antes
                    string originalPath = string.Empty;
                    if (saveOriginals)
                    {
                        originalPath = await _fileStorage.SaveFileAsync(
                            pdfBytes,
                            $"{documentId}_original",
                            "originals",
                            cancellationToken);
                    }

                    // 2.5 Determinar qué páginas eliminar
                    var pagesToRemove = new List<int>();
                    bool anyPageRemoved = false;

                    if (processingOptions.AutoRemoveAllSeparators)
                    {
                        // MODO AUTOMÁTICO: analizar TODAS las páginas y eliminar las que contengan separadores
                        _logger.LogInformation("Modo automático activado: analizando todas las páginas del documento {DocumentId}", documentId);

                        int totalPages = await _pdfManipulator.GetPageCountAsync(pdfBytes, cancellationToken);
                        for (int pageIdx = 0; pageIdx < totalPages; pageIdx++)
                        {
                            PageAnalysisResult pageResult = await _pdfAnalyzer.AnalyzePageAsync(pdfBytes, pageIdx, analysisRules, cancellationToken);
                            if (pageResult.ShouldRemove)
                            {
                                pagesToRemove.Add(pageIdx);
                                if (processingOptions.ShowExtractedText && !string.IsNullOrEmpty(pageResult.ExtractedTextPreview))
                                {
                                    _logger.LogDebug("Página {Page} contiene separador. Texto extraído: {Text}",
                                        pageIdx + 1, pageResult.ExtractedTextPreview);
                                }
                            }
                        }
                        _logger.LogInformation("Se eliminarán {Count} páginas de {Total}", pagesToRemove.Count, totalPages);
                    }
                    else if (processingOptions.RemovePagesEnabled)
                    {
                        // MODO MANUAL: obtener las páginas según la configuración
                        var candidatePages = ParsePageRange(processingOptions, await _pdfManipulator.GetPageCountAsync(pdfBytes, cancellationToken));

                        if (processingOptions.AnalyzeAllPages)
                        {
                            // Analizar cada página candidata y solo eliminar las que cumplan criterios
                            foreach (int pageIdx in candidatePages)
                            {
                                var pageResult = await _pdfAnalyzer.AnalyzePageAsync(pdfBytes, pageIdx, analysisRules, cancellationToken);
                                if (pageResult.ShouldRemove)
                                {
                                    pagesToRemove.Add(pageIdx);
                                    if (processingOptions.ShowExtractedText && !string.IsNullOrEmpty(pageResult.ExtractedTextPreview))
                                    {
                                        _logger.LogDebug("Página {Page} (candidata) contiene separador. Texto: {Text}",
                                            pageIdx + 1, pageResult.ExtractedTextPreview);
                                    }
                                }
                                else
                                {
                                    _logger.LogDebug("Página {Page} (candidata) NO contiene separador, se conserva", pageIdx + 1);
                                }
                            }
                        }
                        else
                        {
                            // Eliminar todas las páginas candidatas sin análisis
                            pagesToRemove.AddRange(candidatePages);
                        }
                    }

                    // 2.6 Remover páginas si es necesario
                    byte[] processedPdf = pdfBytes;
                    if (pagesToRemove.Any())
                    {
                        processedPdf = await _pdfManipulator.RemovePagesAsync(pdfBytes, pagesToRemove.Distinct().OrderBy(x => x), cancellationToken);
                        if (processedPdf.Length != pdfBytes.Length)
                        {
                            anyPageRemoved = true;
                            pagesRemoved += pagesToRemove.Count;
                            _logger.LogInformation("Se eliminaron {Count} páginas del documento {DocumentId}", pagesToRemove.Count, documentId);
                        }
                    }

                    // 2.7 Actualizar en API si corresponde
                    bool apiUpdated = false;
                    if (updateApi && anyPageRemoved)
                    {
                        _logger.LogDebug("Subiendo PDF procesado a la API para documento {DocumentId}", documentId);
                        apiUpdated = await _apiClient.UploadPdfAsync(
                                                      config.ApiBaseUrl,
                                                      config.BearerToken,
                                                      documentName,
                                                      processedPdf,
                                                      document.DocumentTypeId,
                                                      versionId,
                                                      originalData,
                                                      originalFilesName,
                                                      cancellationToken);

                        if (apiUpdated)
                            updated++;
                        else
                            _logger.LogWarning("Fallo al actualizar documento {DocumentId} en API", documentId);
                    }

                    if (processingOptions.CreateBackup)
                    {

                        // 2.8 Guardar PDF procesado
                        // Guardar procesado
                        string processedPath = await _fileStorage.SaveFileAsync(
                            processedPdf,
                            $"{documentId}_processed",
                            "processed",
                            cancellationToken);

                        
                    }

                    await LogAsync("SUCCESS", "Procesamiento",
                        anyPageRemoved ? "Documento procesado (páginas removidas)" : "Documento procesado (sin cambios)",
                        documentId);

                    processed++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error procesando documento {DocumentId} en sesión {SessionId}", documentId, sessionId);
                    await LogAsync("ERROR", "Procesamiento", $"Error: {ex.Message}", documentId);

                }

                // Actualizar progreso después de cada documento
                var afterProgress = new ProcessProgressDto
                {
                    Total = total,
                    Processed = processed,
                    Updated = updated,
                    PagesRemoved = pagesRemoved,
                    Errors = errors,
                    Skipped = skipped,
                    CurrentDocumentId = documentId,
                    CurrentDocumentName = documentName,
                    Status = $"Procesados {processed}/{total}"
                };
                _progressStore.UpdateProgress(sessionId, afterProgress);
            }

            // Progreso final
            var finalProgress = new ProcessProgressDto
            {
                Total = total,
                Processed = processed,
                Updated = updated,
                PagesRemoved = pagesRemoved,
                Errors = errors,
                Skipped = skipped,
                Status = "Completado"
            };
            _progressStore.UpdateProgress(sessionId, finalProgress);

            string summary = $"Procesamiento finalizado. Procesados: {processed}, Actualizados: {updated}, Páginas removidas: {pagesRemoved}, Errores: {errors}, Saltados: {skipped}";
            _logger.LogInformation(summary);
            await LogAsync("INFO", "Procesamiento", summary);
        }

        // Método auxiliar para registrar en log y BD
        private async Task LogAsync(string level, string category, string message, string documentId = null)
        {
            _logger.Log(GetLogLevel(level), message);
            
        }

        private static LogLevel GetLogLevel(string level) => level switch
        {
            "ERROR" => LogLevel.Error,
            "WARNING" => LogLevel.Warning,
            "SUCCESS" => LogLevel.Information,
            _ => LogLevel.Information
        };
        private byte[] DataUrlToBytes(string dataUrl)
        {
            var base64Data = dataUrl.Substring(dataUrl.IndexOf(",") + 1);
            return Convert.FromBase64String(base64Data);
        }

        // Método para parsear la configuración de páginas
        private List<int> ParsePageRange(ProcessingOptions options, int totalPages)
        {
            var pages = new List<int>();
            if (options.RemoveMode == "specific" && !string.IsNullOrWhiteSpace(options.PagesToRemove))
            {
                var parts = options.PagesToRemove.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.Contains('-'))
                    {
                        var range = trimmed.Split('-');
                        if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                        {
                            start = Math.Max(1, start);
                            end = Math.Min(totalPages, end);
                            for (int p = start; p <= end; p++)
                                pages.Add(p - 1); // convertir a 0-based
                        }
                    }
                    else if (int.TryParse(trimmed, out int single))
                    {
                        if (single >= 1 && single <= totalPages)
                            pages.Add(single - 1);
                    }
                }
            }
            else if (options.RemoveMode == "range")
            {
                int start = Math.Max(1, options.PageRangeStart);
                int end = Math.Min(totalPages, options.PageRangeEnd);
                for (int p = start; p <= end; p++)
                    pages.Add(p - 1);
            }
            return pages.Distinct().OrderBy(x => x).ToList();
        }
        
    }
}
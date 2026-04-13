using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using System.Text.Json;

namespace ProDoctivityDS.Application.Services
{
    public class CompleteDocumentInfoService : ICompleteDocumentInfoService
    {
        private readonly IProductivityApiClient _apiClient;
        private readonly IStoredConfigurationRepository _configRepository;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<CompleteDocumentInfoService> _logger;

        public CompleteDocumentInfoService(
            IProductivityApiClient apiClient,
            IStoredConfigurationRepository configRepository,
            IFileStorageService fileStorage,
            ILogger<CompleteDocumentInfoService> logger)
        {
            _apiClient = apiClient;
            _configRepository = configRepository;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ==================== MÉTODOS PÚBLICOS ====================

        public async Task<DocumentsByCedulaResponseDto> GetDocumentsByCedulaAsync(string cedula, CancellationToken cancellationToken)
        {
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config == null)
                throw new InvalidOperationException("No hay configuración activa.");

            await _apiClient.EnsureValidTokenAsync(cancellationToken);

            var docsByCedula = await SearchDocumentsByQueryAsync(config, cedula, cancellationToken);
            if (docsByCedula.Count == 0)
                throw new Exception($"No se encontraron documentos que contengan la cédula {cedula}");

            var firstDoc = docsByCedula.First();
            string fullName = firstDoc.Name;

            var docsByName = await SearchDocumentsByQueryAsync(config, fullName, cancellationToken);

            var documentsList = new List<DocumentForCompletionDto>();
            int withCedulaCount = 0, withoutCedulaCount = 0;

            foreach (var doc in docsByName)
            {
                var idNumber = await GetIdentityNumberAsync(doc.DocumentId, config, cancellationToken);
                bool hasId = !string.IsNullOrEmpty(idNumber);
                if (hasId) withCedulaCount++;
                else withoutCedulaCount++;

                

                documentsList.Add(new DocumentForCompletionDto
                {
                    DocumentId = doc.DocumentId,
                    Name = doc.Name,
                    DocumentTypeName = doc.DocumentTypeName,
                    HasIdentityNumber = hasId,
                    IdentityNumber = idNumber,
                    CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(doc.CreatedAt).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss")
                });
            }

            return new DocumentsByCedulaResponseDto
            {
                Cedula = cedula,
                FullName = fullName,
                Documents = documentsList,
                DocumentsWithCedulaCount = withCedulaCount,
                DocumentsWithoutCedulaCount = withoutCedulaCount
            };
        }

        public async Task<CompleteDocumentInfoResponseDto> CompleteMissingDocumentsAsync(string cedula, CancellationToken cancellationToken)
        {
            var config = await _configRepository.GetActiveConfigurationAsync();
            if (config == null)
                throw new InvalidOperationException("No hay configuración activa.");

            await _apiClient.EnsureValidTokenAsync(cancellationToken);

            var docsByCedula = await SearchDocumentsByQueryAsync(config, cedula, cancellationToken);
            if (docsByCedula.Count == 0)
                throw new Exception($"No se encontraron documentos con la cédula {cedula}");

            string fullName = docsByCedula.First().Name;
            var docsByName = await SearchDocumentsByQueryAsync(config, fullName, cancellationToken);

            var updatedIds = new List<string>();
            var errors = new List<string>();

            foreach (var doc in docsByName)
            {
                var existingNumber = await GetIdentityNumberAsync(doc.DocumentId, config, cancellationToken);
                if (!string.IsNullOrEmpty(existingNumber))
                    continue;

                try
                {
                    // Obtener última versión y detalles
                    var versions = await _apiClient.GetDocumentVersionsAsync(
                        config.ApiBaseUrl, config.BearerToken, doc.DocumentId, cancellationToken);
                    var lastVersion = versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                    if (lastVersion == null) continue;

                    var versionDetail = await _apiClient.GetDocumentVersionDetailAsync(
                        config.ApiBaseUrl, config.BearerToken, doc.DocumentId, lastVersion.DocumentVersionId, cancellationToken);

                    var pdfDataUrl = versionDetail?.Document?.Binaries?
                        .FirstOrDefault(b => b.Contains("application/pdf") || b.Contains("application/octet-stream"));
                    if (string.IsNullOrEmpty(pdfDataUrl)) continue;
                    byte[] pdfBytes = DataUrlToBytes(pdfDataUrl);

                    // ========== GUARDAR COPIA DE SEGURIDAD ==========
                    try
                    {
                        string backupPath = await _fileStorage.SaveFileAsync(
                            pdfBytes,
                            $"{doc.DocumentId}_original_{DateTime.Now:yyyyMMddHHmmss}",
                            "completed_info",
                            cancellationToken);
                        _logger.LogInformation("Copia de seguridad guardada en: {BackupPath}", backupPath);
                    }
                    catch (Exception backupEx)
                    {
                        _logger.LogWarning(backupEx, "No se pudo guardar copia de seguridad para documento {DocumentId}", doc.DocumentId);
                        // Continuamos con la actualización aunque falle el backup
                    }

                    // Preservar metadatos originales
                    var originalData = versionDetail.Document.Data;
                    var originalFilesName = versionDetail.Document.FilesName;

                    var dataDict = originalData as Dictionary<string, object>
                                   ?? JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(originalData));
                    if (dataDict == null) dataDict = new Dictionary<string, object>();
                    dataDict["numeroDocumentoIdentidad"] = cedula;
                    object updatedData = dataDict;

                    // Subir nueva versión
                    var success = await _apiClient.UploadPdfAsync(
                        config.ApiBaseUrl, config.BearerToken,
                        doc.Name, pdfBytes, doc.DocumentTypeId,
                        lastVersion.DocumentVersionId,
                        updatedData,
                        originalFilesName,
                        cancellationToken);

                    if (success)
                        updatedIds.Add(doc.DocumentId);
                    else
                        errors.Add($"Error al actualizar documento {doc.DocumentId}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{doc.DocumentId}: {ex.Message}");
                    _logger.LogError(ex, "Error actualizando documento {DocumentId}", doc.DocumentId);
                }
            }

            int docsWithCedulaCount = 0;
            foreach (var doc in docsByName)
            {
                var id = await GetIdentityNumberAsync(doc.DocumentId, config, cancellationToken);
                if (!string.IsNullOrEmpty(id)) docsWithCedulaCount++;
            }

            return new CompleteDocumentInfoResponseDto
            {
                Cedula = cedula,
                FullName = fullName,
                DocumentsFoundWithCedula = docsWithCedulaCount,
                DocumentsWithoutCedulaUpdated = updatedIds.Count,
                UpdatedDocumentIds = updatedIds,
                Errors = errors
            };
        }

        // ==================== MÉTODOS AUXILIARES ====================

        private async Task<List<POSTDocumentDto>> SearchDocumentsByQueryAsync(StoredConfiguration config, string query, CancellationToken cancellationToken)
        {
            var result = new List<POSTDocumentDto>();
            int page = 0;
            const int pageSize = 100;
            while (true)
            {
                var (docs, total) = await _apiClient.GetDocumentsAsync(
                    config.ApiBaseUrl, config.BearerToken,
                    documentTypeIds: null,
                    query: query,
                    page: page,
                    pageSize: pageSize,
                    config.ApiKey, config.ApiSecret, config.CookieSessionId,
                    cancellationToken);
                result.AddRange(docs);
                if (result.Count >= total || docs.Count < pageSize)
                    break;
                page++;
            }
            return result;
        }

        private async Task<string?> GetIdentityNumberAsync(string documentId, StoredConfiguration config, CancellationToken cancellationToken)
        {
            try
            {
                var versions = await _apiClient.GetDocumentVersionsAsync(
                    config.ApiBaseUrl, config.BearerToken, documentId, cancellationToken);
                var lastVersion = versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
                if (lastVersion == null) return null;

                var detail = await _apiClient.GetDocumentVersionDetailAsync(
                    config.ApiBaseUrl, config.BearerToken, documentId, lastVersion.DocumentVersionId, cancellationToken);

                if (detail?.Document?.Data == null) return null;

                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(detail.Document.Data));
                if (data != null && data.TryGetValue("numeroDocumentoIdentidad", out var value))
                    return value?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo identity number para documento {DocumentId}", documentId);
            }
            return null;
        }

        private byte[] DataUrlToBytes(string dataUrl)
        {
            var base64Data = dataUrl.Substring(dataUrl.IndexOf(",") + 1);
            return Convert.FromBase64String(base64Data);
        }
    }
}
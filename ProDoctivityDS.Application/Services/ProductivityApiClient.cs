using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ProDoctivityDS.Application.Services
{
    public class ProductivityApiClient : IProductivityApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductivityApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProductivityApiClient(
            IHttpClientFactory httpClientFactory,
            ILogger<ProductivityApiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        private HttpClient CreateClient(string baseUrl, string bearerToken, string? apiKey = null, string? apiSecret = null, string? cookie = null)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            if (!string.IsNullOrEmpty(apiSecret))
                client.DefaultRequestHeaders.Add("x-api-secret", apiSecret);
            if (!string.IsNullOrEmpty(bearerToken))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            if (!string.IsNullOrEmpty(cookie))
                client.DefaultRequestHeaders.Add("Cookie", cookie);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        // ---------- GetDocumentsAsync ----------
        public async Task<(List<ProductivityDocumentDto> Documents, int TotalCount)> GetDocumentsAsync(
            string baseUrl,
            string bearerToken,
            List<string>? documentTypeIds,
            string? name,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Construir URL con parámetros de búsqueda
                var url = $"app/documents?dateStart=0&pageNumber={page}&rowsPerPage={pageSize}&sortField=updatedAt&sortDirection=DESC";
                if (!string.IsNullOrWhiteSpace(name))
                    url += $"&name={Uri.EscapeDataString(name)}";

                var client = CreateClient(baseUrl, bearerToken);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                // Si hay filtros de tipo, enviarlos en el body como JSON (según API actual)
                if (documentTypeIds != null && documentTypeIds.Any())
                {
                    var payload = new Dictionary<string, object>
                    {
                        ["documentTypeIdList[]"] = documentTypeIds
                    };
                    string jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResponse = JsonSerializer.Deserialize<ProductivitySearchResponseDto>(json, _jsonOptions);

                var documents = searchResponse?.Documents?
                    .Select(d => d)
                    .ToList();

                var totalCount = searchResponse?.Total ?? documents.Count;
                return (documents, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetDocumentsAsync");
                throw;
            }
        }

        // ---------- GetDocumentAsync ----------
        public async Task<ProductivityDocumentDto> GetDocumentAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"documents/{documentId}";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonSerializer.Deserialize<ProductivityDocumentDto>(json, _jsonOptions);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documento {DocumentId}", documentId);
                throw;
            }
        }

        // ---------- GetDocumentVersionsAsync ----------
        
        public async Task<List<ProductivityVersionDto>> GetDocumentVersionsAsync(
            string baseUrl,
            string bearerToken,
            string documentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"documents/{documentId}/versions";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var versions = JsonSerializer.Deserialize<List<ProductivityVersionDto>>(json, _jsonOptions);
                return versions?
                    .Select(v => new ProductivityVersionDto
                    {
                        DocumentVersionId = v.DocumentVersionId ?? v.Id ?? string.Empty,
                        Version = v.Version,
                        CreatedAt = v.CreatedAt,
                        Binaries = v.Binaries
                    })
                    .ToList() ?? new List<ProductivityVersionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener versiones del documento {DocumentId}", documentId);
                throw;
            }
        }

        // ---------- DownloadPdfAsync ----------
        public async Task<byte[]> DownloadPdfAsync(
            string baseUrl,
            string bearerToken,
            string versionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = $"documents/versions/{versionId}"; // Asumiendo endpoint directo
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var detail = JsonSerializer.Deserialize<ProductivityDocumentDetailDto>(json, _jsonOptions);

                // Buscar el primer binario PDF en data URL
                var binaries = detail?.Binaries ?? detail?.Document?.Data as List<string> ?? new List<string>();
                var pdfDataUrl = binaries.FirstOrDefault(b => b.StartsWith("data:application/pdf") || b.StartsWith("data:application/octet-stream"));

                if (string.IsNullOrEmpty(pdfDataUrl))
                {
                    _logger.LogWarning("No se encontró PDF en los binarios de la versión {VersionId}", versionId);
                    return Array.Empty<byte>();
                }

                return DataUrlToBytes(pdfDataUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar PDF de versión {VersionId}", versionId);
                throw;
            }
        }

        // ---------- UploadPdfAsync ----------
        public async Task<bool> UploadPdfAsync(
            string baseUrl,
            string bearerToken,
            string fileName,
            byte[] pdfContent,
            string documentTypeId,
            string? parentVersionId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient(baseUrl, bearerToken);
                var url = "app/documents";

                // Convertir PDF a Data URL
                var dataUrl = BytesToDataUrl(pdfContent, "application/pdf");

                var payload = new ProductivityUploadRequestDto
                {
                    DocumentTypeId = documentTypeId,
                    ContentType = "application/pdf",
                    Data = new { }, // Si se requieren metadatos adicionales, se pueden agregar
                    Documents = new List<string> { dataUrl },
                    MustUpdateBinaries = true,
                    ParentDocumentVersionId = parentVersionId,
                    FilesName = new List<string> { fileName },
                    OriginMethod = "imported"
                };

                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("PDF subido exitosamente. Nombre: {FileName}", fileName);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error al subir PDF. Status: {StatusCode}, Respuesta: {Error}", response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir PDF {FileName}", fileName);
                return false;
            }
        }

        private byte[] DataUrlToBytes(string dataUrl)
        {
            var parts = dataUrl.Split(new[] { ',' }, 2);
            if (parts.Length < 2)
                return Array.Empty<byte>();

            var base64 = parts[1];
            return Convert.FromBase64String(base64);
        }

        private string BytesToDataUrl(byte[] bytes, string mimeType)
        {
            var base64 = Convert.ToBase64String(bytes);
            return $"data:{mimeType};base64,{base64}";
        }

        
    }
}
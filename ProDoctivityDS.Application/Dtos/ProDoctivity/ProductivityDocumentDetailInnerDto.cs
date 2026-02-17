using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class ProductivityDocumentDetailInnerDto
    {
        [JsonPropertyName("documentTypeId")]
        public string? DocumentTypeId { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; } // Metadatos adicionales

        [JsonPropertyName("filesName")]
        public List<string>? FilesName { get; set; }

        [JsonPropertyName("originMethod")]
        public string? OriginMethod { get; set; }
    }
}

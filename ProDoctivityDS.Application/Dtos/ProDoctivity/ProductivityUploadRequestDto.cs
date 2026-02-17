using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{

    public class ProductivityUploadRequestDto
    {
        [JsonPropertyName("documentTypeId")]
        public string DocumentTypeId { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/pdf";

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("documents")]
        public List<string> Documents { get; set; } = new(); // Data URLs

        [JsonPropertyName("mustUpdateBinaries")]
        public bool MustUpdateBinaries { get; set; } = true;

        [JsonPropertyName("parentDocumentVersionId")]
        public string? ParentDocumentVersionId { get; set; }

        [JsonPropertyName("filesName")]
        public List<string> FilesName { get; set; } = new();

        [JsonPropertyName("originMethod")]
        public string OriginMethod { get; set; } = "imported";
    }
}

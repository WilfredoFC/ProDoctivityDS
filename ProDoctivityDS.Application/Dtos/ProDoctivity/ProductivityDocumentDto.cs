using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{

    public class ProductivityDocumentDto
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string? Id { get; set; } // Alternativo, a veces viene como "id"

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("documentTypeId")]
        public string DocumentTypeId { get; set; } = string.Empty;

        [JsonPropertyName("documentTypeName")]
        public string DocumentTypeName { get; set; } = string.Empty;

        [JsonPropertyName("lastDocumentVersionId")]
        public string? LastDocumentVersionId { get; set; }

        [JsonPropertyName("documentVersionId")]
        public string? DocumentVersionId { get; set; }

        [JsonPropertyName("createdAt")]
        public long? CreatedAt { get; set; } // Timestamp Unix

        [JsonPropertyName("updatedAt")]
        public long? UpdatedAt { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }
    }
}

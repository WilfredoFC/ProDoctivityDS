using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class POSTDocumentDto
    {
        //[JsonPropertyName("$entityType")]
        //public string? EntityType { get; set; }

        [JsonPropertyName("$name")]
        public string? Name { get; set; }

        [JsonPropertyName("$documentId")]
        public string? DocumentId { get; set; }

        [JsonPropertyName("$documentVersionId")]
        public string? DocumentVersionId { get; set; }

        [JsonPropertyName("$createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("$documentTypeName")]
        public string? DocumentTypeName { get; set; }

        [JsonPropertyName("$documentTypeId")]
        public string? DocumentTypeId { get; set; }

        //[JsonPropertyName("$updatedAt")]
        //public long UpdatedAt { get; set; }

        //[JsonPropertyName("$createdBy")]
        //public string? CreatedBy { get; set; }

        //[JsonPropertyName("$updatedBy")]
        //public string? UpdatedBy { get; set; }

        //[JsonPropertyName("$documentDate")]
        //public long DocumentDate { get; set; }

        //[JsonPropertyName("$expirationDate")]
        //public long? ExpirationDate { get; set; }

        //[JsonPropertyName("$documentTypeVersionId")]
        //public string? DocumentTypeVersionId { get; set; }

        //[JsonPropertyName("$generationToken")]
        //public object? GenerationToken { get; set; }

        //[JsonPropertyName("$templateId")]
        //public object? TemplateId { get; set; }

        //[JsonPropertyName("$templateVersionId")]
        //public object? TemplateVersionId { get; set; }

        //[JsonPropertyName("$normalized")]
        //public string? Normalized { get; set; }

        //[JsonPropertyName("$score")]
        //public double Score { get; set; }
    }
}

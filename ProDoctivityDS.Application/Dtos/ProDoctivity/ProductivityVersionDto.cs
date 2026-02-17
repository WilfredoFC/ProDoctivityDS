using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{

    public class ProductivityVersionDto
    {
        [JsonPropertyName("documentVersionId")]
        public string DocumentVersionId { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("createdAt")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("binaries")]
        public List<string>? Binaries { get; set; } // Data URLs
    }
}


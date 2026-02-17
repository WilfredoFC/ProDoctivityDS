using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{

    public class ProductivitySearchResponseDto
    {
        [JsonPropertyName("documents")]
        public List<ProductivityDocumentDto> Documents { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("rowsPerPage")]
        public int RowsPerPage { get; set; }
    }
}

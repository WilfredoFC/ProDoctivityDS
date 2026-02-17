using System.Text.Json.Serialization;

namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{

    public class ProductivityDocumentDetailDto
    {
        [JsonPropertyName("document")]
        public ProductivityDocumentDetailInnerDto? Document { get; set; }

        [JsonPropertyName("binaries")]
        public List<string>? Binaries { get; set; }
    }

    
}

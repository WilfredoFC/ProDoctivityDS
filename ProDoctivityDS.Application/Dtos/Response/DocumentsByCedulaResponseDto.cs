using ProDoctivityDS.Application.Dtos.ProDoctivity;

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class DocumentsByCedulaResponseDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<DocumentForCompletionDto> Documents { get; set; } = new();
        public int DocumentsWithCedulaCount { get; set; }
        public int DocumentsWithoutCedulaCount { get; set; }
    }
}

namespace ProDoctivityDS.Application.Dtos.Response
{
    public class CompleteDocumentInfoResponseDto
    {
        public string Cedula { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int DocumentsFoundWithCedula { get; set; }
        public int DocumentsWithoutCedulaUpdated { get; set; }
        public List<string> UpdatedDocumentIds { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}

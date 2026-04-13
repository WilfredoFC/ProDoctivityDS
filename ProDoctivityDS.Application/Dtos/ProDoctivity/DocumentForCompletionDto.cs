namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class DocumentForCompletionDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DocumentTypeName { get; set; } = string.Empty;
        public bool HasIdentityNumber { get; set; }
        public string? IdentityNumber { get; set; }
        public string? FileSize { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}

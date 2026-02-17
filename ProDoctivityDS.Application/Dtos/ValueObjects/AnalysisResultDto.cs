namespace ProDoctivityDS.Domain.Entities.ValueObjects
{
    public class AnalysisResultDto
    {
        public bool ShouldRemove { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? NormalizedText { get; set; }
        public int PageCount { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    }
}

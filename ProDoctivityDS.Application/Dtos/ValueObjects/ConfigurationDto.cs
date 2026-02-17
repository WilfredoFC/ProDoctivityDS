using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Dtos.ValueObjects
{
    public class ConfigurationDto
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string BearerToken { get; set; } = string.Empty;
        public string CookieSessionId { get; set; } = string.Empty;
        public ProcessingOptionsDto ProcessingOptions { get; set; } = new();
        public AnalysisRuleSetDto AnalysisRules { get; set; } = new();
    }
}

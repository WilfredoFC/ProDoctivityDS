using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Dtos.ValueObjects
{
    public class SaveConfigurationRequestDto
    {
        public ApiCredentialsDto ApiCredentials { get; set; } = new();
        public ProcessingOptionsDto ProcessingOptions { get; set; } = new();
        public AnalysisRuleSetDto AnalysisRules { get; set; } = new();
    }
}

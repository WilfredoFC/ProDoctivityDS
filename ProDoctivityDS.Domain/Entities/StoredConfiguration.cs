using ProDoctivityDS.Domain.Base;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ProDoctivityDS.Domain.Entities
{
    [Table("StoredConfigurations")]
    public class StoredConfiguration : BaseEntity
    {

        [Required]
        public string ApiBaseUrl { get; set; } = string.Empty;

        // Campos sensibles (se cifrarán en la capa de aplicación)
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string BearerToken { get; set; } = string.Empty;
        public string CookieSessionId { get; set; } = string.Empty;

        // Almacenamiento JSON
        public string ProcessingOptionsJson { get; set; } = "{}";
        public string AnalysisRulesJson { get; set; } = "{}";

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Propiedades no mapeadas para trabajar con objetos fuertemente tipados
        [NotMapped]
        public ProcessingOptions ProcessingOptions
        {
            get => JsonSerializer.Deserialize<ProcessingOptions>(ProcessingOptionsJson ?? "{}")
                   ?? new ProcessingOptions();
            set => ProcessingOptionsJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public AnalysisRuleSet AnalysisRules
        {
            get => JsonSerializer.Deserialize<AnalysisRuleSet>(AnalysisRulesJson ?? "{}")
                   ?? new AnalysisRuleSet();
            set => AnalysisRulesJson = JsonSerializer.Serialize(value);
        }
    }
}

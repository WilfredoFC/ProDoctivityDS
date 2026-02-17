namespace ProDoctivityDS.Domain.Entities.ValueObjects
{
    public class AnalysisRuleSetDto
    {
        public CriterionDto Criterion1 { get; set; } = new CriterionDto
        {
            Text = "SEPARADOR DE DOCUMENTOS",
            IsRegex = false
        };

        public CriterionDto Criterion2 { get; set; } = new CriterionDto
        {
            Text = "DOC-001",
            IsRegex = false
        };

        public NormalizationOptionsDto Normalization { get; set; } = new NormalizationOptionsDto();

        // Reservado para futuros cambios (OR es el único por ahora)
        public string EvaluationLogic { get; set; } = "Or";
    }
}

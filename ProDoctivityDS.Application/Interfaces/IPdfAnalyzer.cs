using ProDoctivityDS.Domain.Entities.ValueObjects;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IPdfAnalyzer
    {
        /// <summary>
        /// Extrae el texto completo de la primera página de un PDF.
        /// </summary>
        Task<string> ExtractFirstPageTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aplica las reglas de normalización a un texto.
        /// </summary>
        string NormalizeText(string text, NormalizationOptionsDto options);

        /// <summary>
        /// Evalúa si el texto de la primera página cumple los criterios (OR).
        /// </summary>
        bool ShouldRemoveFirstPage(string firstPageText, AnalysisRuleSetDto rules);

        /// <summary>
        /// Analiza un PDF completo y retorna un diagnóstico detallado.
        /// </summary>
        Task<AnalysisResultDto> AnalyzePdfAsync(byte[] pdfBytes, AnalysisRuleSetDto rules, CancellationToken cancellationToken = default);
    }
}

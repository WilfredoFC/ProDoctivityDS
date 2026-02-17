using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace ProDoctivityDS.Application.Services
{

    public class PdfAnalyzerService : IPdfAnalyzer
    {
        private readonly ILogger<PdfAnalyzerService> _logger;

        public PdfAnalyzerService(ILogger<PdfAnalyzerService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> ExtractFirstPageTextAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new MemoryStream(pdfBytes);
                    using var pdf = PdfDocument.Open(stream);

                    if (pdf.NumberOfPages == 0)
                        return string.Empty;

                    var page = pdf.GetPage(1);
                    return page.Text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al extraer texto de la primera página del PDF");
                    return string.Empty;
                }
            }, cancellationToken);
        }

        /// <inheritdoc />
        public string NormalizeText(string text, NormalizationOptionsDto options)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (!options.IsEnabled)
                return text;

            var normalized = text;

            // 1. Convertir a mayúsculas
            if (options.ToUpperCase)
                normalized = normalized.ToUpperInvariant();

            // 2. Remover acentos (diacríticos)
            if (options.RemoveAccents)
            {
                var normalizedForm = normalized.Normalize(NormalizationForm.FormD);
                var chars = normalizedForm.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
                normalized = new string(chars).Normalize(NormalizationForm.FormC);
            }

            // 3. Ignorar saltos de línea (reemplazar por espacio)
            if (options.IgnoreLineBreaks)
                normalized = normalized.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // 4. Remover signos de puntuación (todo excepto letras, dígitos y espacios)
            if (options.RemovePunctuation)
                normalized = Regex.Replace(normalized, @"[^\p{L}\p{N}\s]", " ");

            // 5. Remover espacios extras (múltiples espacios, tabs, etc.)
            if (options.TrimExtraSpaces)
                normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <inheritdoc />
        public bool ShouldRemoveFirstPage(string firstPageText, AnalysisRuleSetDto rules)
        {
            if (string.IsNullOrEmpty(firstPageText))
                return false;

            // Aplicar normalización global si está habilitada
            var textToEvaluate = rules.Normalization.IsEnabled
                ? NormalizeText(firstPageText, rules.Normalization)
                : firstPageText;

            // Si no se aplicó normalización pero ToUpperCase está activo (por compatibilidad con código legacy)
            // Nota: En NormalizationOptions, ToUpperCase es parte de las opciones. Ya se aplicó si IsEnabled=true.
            // Si IsEnabled=false, no se normaliza nada, respetamos case-sensitive según lo que indique el criterio.

            bool result1 = EvaluateCriterion(textToEvaluate, rules.Criterion1, rules.Normalization);
            bool result2 = EvaluateCriterion(textToEvaluate, rules.Criterion2, rules.Normalization);

            // Por ahora solo soportamos OR
            return result1 || result2;
        }

        private bool EvaluateCriterion(string text, CriterionDto criterion, NormalizationOptionsDto normalization)
        {
            if (criterion == null || string.IsNullOrEmpty(criterion.Text))
                return false;

            string searchText = criterion.Text;

            // Si la normalización global está activada, normalizamos también el texto del criterio
            if (normalization.IsEnabled)
            {
                searchText = NormalizeText(criterion.Text, normalization);
            }
            else
            {
                // Si no hay normalización, respetamos la configuración de case sensitivity
                // En el modelo actual, si ToUpperCase=false y IsEnabled=false, no convertimos nada.
                // Pero para mantener compatibilidad con el WinForms, si !CaseSensitive entonces ToUpper.
                // Como no tenemos esa propiedad directamente, asumimos que si no se normaliza,
                // la comparación será case-sensitive a menos que el usuario use regex con opción IgnoreCase.
                // En la práctica, en el formulario original, si CaseSensitive=false se aplicaba ToUpperInvariant.
                // Para simplificar, aquí no aplicamos conversión automática; delegamos en el método Contains.
            }

            if (criterion.IsRegex)
            {
                try
                {
                    var options = RegexOptions.None;
                    // Si la normalización no está activada pero el usuario quiere insensibilidad, debería haber usado ToUpperCase.
                    // Podríamos agregar una propiedad RegexOptions, pero por ahora asumimos que el patrón ya incluye (?i) si es necesario.
                    return Regex.IsMatch(text, searchText, options);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // Búsqueda de texto simple
                var comparison = StringComparison.Ordinal; // case-sensitive por defecto
                                                           // Si la normalización (ToUpperCase) se aplicó, el texto ya está en mayúsculas y el criterio también.
                                                           // Entonces la comparación será efectivamente case-insensitive porque ambos están en mayúsculas.
                                                           // Si no se normalizó, respetamos mayúsculas/minúsculas.
                return text.Contains(searchText, comparison);
            }
        }

        /// <inheritdoc />
        public async Task<AnalysisResultDto> AnalyzePdfAsync(byte[] pdfBytes, AnalysisRuleSetDto rules, CancellationToken cancellationToken = default)
        {
            var result = new AnalysisResultDto();

            try
            {
                // Extraer texto de la primera página
                var firstPageText = await ExtractFirstPageTextAsync(pdfBytes, cancellationToken);

                // Aplicar normalización para diagnóstico
                var normalizedText = rules.Normalization.IsEnabled
                    ? NormalizeText(firstPageText, rules.Normalization)
                    : firstPageText;

                result.NormalizedText = normalizedText.Length > 500 ? normalizedText[..500] + "..." : normalizedText;

                // Determinar si debe removerse
                result.ShouldRemove = ShouldRemoveFirstPage(firstPageText, rules);

                // Construir diagnóstico detallado
                var diagnosis = new StringBuilder();

                if (string.IsNullOrEmpty(firstPageText))
                {
                    diagnosis.Append("PDF vacío o sin texto en primera página. ");
                }
                else
                {
                    bool c1 = EvaluateCriterion(
                        rules.Normalization.IsEnabled ? normalizedText : firstPageText,
                        rules.Criterion1,
                        rules.Normalization);
                    bool c2 = EvaluateCriterion(
                        rules.Normalization.IsEnabled ? normalizedText : firstPageText,
                        rules.Criterion2,
                        rules.Normalization);

                    if (c1)
                        diagnosis.Append($"Criterio 1 ('{Truncate(rules.Criterion1.Text, 30)}') encontrado. ");
                    if (c2)
                        diagnosis.Append($"Criterio 2 ('{Truncate(rules.Criterion2.Text, 30)}') encontrado. ");
                    if (!c1 && !c2)
                        diagnosis.Append("Ningún criterio coincide. ");
                }

                // Contar páginas (para información)
                using var stream = new MemoryStream(pdfBytes);
                using var pdf = PdfDocument.Open(stream);
                result.PageCount = pdf.NumberOfPages;

                diagnosis.Append($"Páginas totales: {result.PageCount}. ");
                diagnosis.Append(result.ShouldRemove ? "Se recomienda REMOVER primera página." : "Se recomienda CONSERVAR primera página.");

                result.Diagnosis = diagnosis.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar PDF");
                result.Diagnosis = $"Error en análisis: {ex.Message}";
                result.ShouldRemove = false;
            }

            return result;
        }

        private string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text[..(maxLength - 3)] + "...";
        }
    }
}

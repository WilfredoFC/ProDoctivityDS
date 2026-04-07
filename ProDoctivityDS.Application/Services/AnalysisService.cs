using AutoMapper;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.Request;
using ProDoctivityDS.Application.Dtos.Response;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities.ValueObjects;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Application.Services
{
    public class AnalysisService : IAnalysisService
    {
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly IPdfAnalyzer _pdfAnalyzer;
        private readonly IMapper _mapper;
        private readonly ILogger<AnalysisService> _logger;

        public AnalysisService(
            IStoredConfigurationRepository configurationRepository,
            IPdfAnalyzer pdfAnalyzer,
            IMapper mapper,
            ILogger<AnalysisService> logger)
        {
            _configurationRepository = configurationRepository;
            _pdfAnalyzer = pdfAnalyzer;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AnalysisRuleSetDto> GetCurrentRulesAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configurationRepository.GetActiveConfigurationAsync();
            var rules = config.AnalysisRules ?? new AnalysisRuleSet();
            return _mapper.Map<AnalysisRuleSetDto>(rules);
        }

        /// <inheritdoc />
        public async Task SaveRulesAsync(AnalysisRuleSetDto rulesDto, CancellationToken cancellationToken = default)
        {
            if (rulesDto == null)
                throw new ArgumentNullException(nameof(rulesDto));

            // Validar que al menos un criterio tenga texto (opcional, pero recomendable)
            if (string.IsNullOrWhiteSpace(rulesDto.Criterion1?.Text) &&
                string.IsNullOrWhiteSpace(rulesDto.Criterion2?.Text))
            {
                throw new ArgumentException("Debe proporcionar al menos un criterio de análisis");
            }

            var config = await _configurationRepository.GetActiveConfigurationAsync();
            config.AnalysisRules = _mapper.Map<AnalysisRuleSet>(rulesDto);
            await _configurationRepository.UpdateConfigurationAsync(config);

            _logger.LogInformation("Reglas de análisis guardadas correctamente");
        }

        /// <inheritdoc />
        public async Task<AnalysisTestResponseDto> TestPdfAsync(TestAnalysisRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request?.FileContent == null || request.FileContent.Length == 0)
                throw new ArgumentException("El contenido del PDF no puede estar vacío");

            var config = await _configurationRepository.GetActiveConfigurationAsync();
            var rules = config.AnalysisRules ?? new AnalysisRuleSet();

            // Realizar el análisis con PdfPig
            var analysisResult = await _pdfAnalyzer.AnalyzePdfAsync(request.FileContent, rules, cancellationToken);

            return new AnalysisTestResponseDto
            {
                ShouldRemove = analysisResult.ShouldRemove,
                Diagnosis = analysisResult.Diagnosis,
                NormalizedText = analysisResult.NormalizedText
            };
        }
    }
}
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.ValueObjects;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;

namespace Application.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IStoredConfigurationRepository _configurationRepository;
        private readonly IActivityLogEntryRepository _logRepository;
        private readonly IProductivityApiClient _apiClient;
        private readonly IMapper _mapper;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(
            IStoredConfigurationRepository configurationRepository,
            IActivityLogEntryRepository logRepository,
            IProductivityApiClient apiClient,
            IMapper mapper,
            ILogger<ConfigurationService> logger)
        {
            _configurationRepository = configurationRepository;
            _logRepository = logRepository;
            _apiClient = apiClient;
            _mapper = mapper;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ConfigurationDto> GetConfigurationDtoAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configurationRepository.GetActiveConfigurationAsync();

            var dto = _mapper.Map<ConfigurationDto>(config);

            // Ocultar credenciales sensibles
            dto.ApiKey = "●●●●●●●●";
            dto.ApiSecret = "●●●●●●●●";
            dto.BearerToken = "●●●●●●●●";
            dto.CookieSessionId = "●●●●●●●●";

            return dto;
        }

        /// <inheritdoc />
        public async Task SaveConfigurationAsync(SaveConfigurationRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Mapear DTO a entidad de dominio
                var config = _mapper.Map<StoredConfiguration>(request);

                // Actualizar en repositorio (el repositorio se encarga del cifrado)
                await _configurationRepository.UpdateConfigurationAsync(config);

                // Registrar en log
                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Category = "Configuración",
                    Message = "Configuración actualizada exitosamente"
                });

                _logger.LogInformation("Configuración guardada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la configuración");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> TestConnectionAsync(ApiCredentialsDto credentials, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Probando conexión a la API de Productivity");

                // Usar el cliente API para intentar obtener un documento (o cualquier endpoint simple)
                // Por simplicidad, intentamos obtener la primera página de documentos con un límite de 1
                var (documents, totalCount) = await _apiClient.GetDocumentsAsync(
                    credentials.BaseUrl,
                    credentials.BearerToken,
                    null,        // Sin filtro de tipos
                    null,        // Sin filtro de nombre
                    0,           // Página 0
                    1,           // Solo 1 documento
                    cancellationToken);

                var success = documents != null;

                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = success ? "SUCCESS" : "ERROR",
                    Category = "Configuración",
                    Message = success
                        ? "Prueba de conexión exitosa"
                        : "Prueba de conexión fallida: no se obtuvieron documentos"
                });

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar la conexión");

                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "ERROR",
                    Category = "Configuración",
                    Message = $"Prueba de conexión fallida: {ex.Message}"
                });

                return false;
            }
        }

        /// <inheritdoc />
        public async Task<ConfigurationDto> ExportConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var config = await _configurationRepository.GetActiveConfigurationAsync();

            // Mapear a DTO (incluye credenciales en texto plano porque el repositorio ya las descifró)
            var dto = _mapper.Map<ConfigurationDto>(config);

            _logger.LogInformation("Configuración exportada");

            return dto;
        }

        /// <inheritdoc />
        public async Task ImportConfigurationAsync(ConfigurationDto importDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Mapear DTO a entidad de dominio
                var config = _mapper.Map<StoredConfiguration>(importDto);

                // Guardar configuración (el repositorio cifrará las credenciales)
                await _configurationRepository.UpdateConfigurationAsync(config);

                await _logRepository.SaveEntityAsync(new ActivityLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Category = "Configuración",
                    Message = "Configuración importada exitosamente"
                });

                _logger.LogInformation("Configuración importada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar configuración");
                throw;
            }
        }
    }
}
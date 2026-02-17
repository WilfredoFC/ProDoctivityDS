using ProDoctivityDS.Application.Dtos.ProDoctivity;
using ProDoctivityDS.Application.Dtos.ValueObjects;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Obtiene la configuración activa para mostrarla en el frontend.
        /// Los campos sensibles (ApiKey, ApiSecret, BearerToken, CookieSessionId)
        /// se retornan como "●●●●●●●●".
        /// </summary>
        Task<ConfigurationDto> GetConfigurationDtoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda la configuración completa (credenciales, opciones de procesamiento y reglas de análisis).
        /// </summary>
        Task SaveConfigurationAsync(SaveConfigurationRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prueba la conexión a la API de Productivity utilizando las credenciales proporcionadas.
        /// </summary>
        Task<bool> TestConnectionAsync(ApiCredentialsDto credentials, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exporta la configuración actual incluyendo las credenciales en texto plano.
        /// Este método se usa para generar el archivo JSON de exportación.
        /// </summary>
        Task<ConfigurationDto> ExportConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Importa una configuración completa desde un DTO (normalmente deserializado de un archivo JSON).
        /// </summary>
        Task ImportConfigurationAsync(ConfigurationDto importDto, CancellationToken cancellationToken = default);
    }
}

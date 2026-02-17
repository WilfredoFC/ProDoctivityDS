using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Domain.Interfaces
{
    public interface IStoredConfigurationRepository : IBaseRepository<StoredConfiguration>
    {
        /// <summary>
        /// Obtiene la configuración activa (única fila). Si no existe, retorna una nueva con valores por defecto.
        /// </summary>
        Task<StoredConfiguration> GetActiveConfigurationAsync();

        /// <summary>
        /// Actualiza la configuración activa (crea si no existe).
        /// </summary>
        Task UpdateConfigurationAsync(StoredConfiguration configuration);
    }
}


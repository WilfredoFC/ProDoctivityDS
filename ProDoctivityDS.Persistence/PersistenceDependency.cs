using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Persistence.Repositories;

namespace ProDoctivityDS.Persistence
{
    public static class PersistenceDependency
    {
        public static void AddPersistenceDependencies(this IServiceCollection services, IConfiguration config)
        {
            // Repositorios
            services.AddSingleton<IStoredConfigurationRepository, StoredConfigurationRepository>();

        }
    }
}

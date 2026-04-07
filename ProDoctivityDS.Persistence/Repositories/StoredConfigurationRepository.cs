using AutoMapper;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Persistence.Repositories
{
    public class StoredConfigurationRepository : IStoredConfigurationRepository
    {
        private readonly IMapper _mapper;
        private StoredConfiguration? _configuration;
        private readonly object _lock = new object();

        public StoredConfigurationRepository(IMapper mapper)
        {
            _mapper = mapper;
        }

        public Task<StoredConfiguration> GetActiveConfigurationAsync()
        {
            lock (_lock)
            {
                if (_configuration == null)
                {
                    return Task.FromResult(new StoredConfiguration());
                }

                var copy = _mapper.Map<StoredConfiguration>(_configuration);

                return Task.FromResult(copy);
            }
        }

        public Task UpdateConfigurationAsync(StoredConfiguration configuration)
        {
            lock (_lock)
            {
                var entityToStore = _mapper.Map<StoredConfiguration>(configuration);

                entityToStore.ProcessingOptionsJson = configuration.ProcessingOptionsJson;
                entityToStore.AnalysisRulesJson = configuration.AnalysisRulesJson;

                entityToStore.LastModified = DateTime.UtcNow;
                _configuration = entityToStore;
            }
            return Task.CompletedTask;
        }
    }
}

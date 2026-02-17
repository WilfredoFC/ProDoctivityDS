using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Persistence.Context;
using ProDoctivityDS.Shared.Interfaces;

namespace ProDoctivityDS.Persistence.Repositories
{
    public class StoredConfigurationRepository : BaseRepository<StoredConfiguration>, IStoredConfigurationRepository
    {
        private readonly ProDoctivityDSDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly IMapper _mapper;

        public StoredConfigurationRepository(ProDoctivityDSDbContext context, IMapper mapper, IEncryptionService encryptionService) : base(context)
        {
            _context = context;
            _mapper = mapper;
            _encryption = encryptionService; 
        }

        public async Task<StoredConfiguration> GetActiveConfigurationAsync()
        {
            var entity = await _context.StoredConfigurations.FirstOrDefaultAsync();
            if (entity == null)
                return new StoredConfiguration();

            // Descifrar campos sensibles
            entity.ApiKey = _encryption.Decrypt(entity.ApiKey);
            entity.ApiSecret = _encryption.Decrypt(entity.ApiSecret);
            entity.BearerToken = _encryption.Decrypt(entity.BearerToken);
            entity.CookieSessionId = _encryption.Decrypt(entity.CookieSessionId);

            return _mapper.Map<StoredConfiguration>(entity);
        }

        public async Task UpdateConfigurationAsync(StoredConfiguration configuration)
        {
            // Mapear a entidad de persistencia
            var entity = _mapper.Map<StoredConfiguration>(configuration);

            // Cifrar campos sensibles
            entity.ApiKey = _encryption.Encrypt(entity.ApiKey);
            entity.ApiSecret = _encryption.Encrypt(entity.ApiSecret);
            entity.BearerToken = _encryption.Encrypt(entity.BearerToken);
            entity.CookieSessionId = _encryption.Encrypt(entity.CookieSessionId);
            entity.LastModified = DateTime.UtcNow;

            var existing = await _context.StoredConfigurations.FirstOrDefaultAsync();
            if (existing == null)
            {
                entity.Id = 1; // forzar singleton
                _context.StoredConfigurations.Add(entity);
            }
            else
            {
                entity.Id = existing.Id;
                _context.Entry(existing).CurrentValues.SetValues(entity);
            }

            await _context.SaveChangesAsync();
        }
    }
}

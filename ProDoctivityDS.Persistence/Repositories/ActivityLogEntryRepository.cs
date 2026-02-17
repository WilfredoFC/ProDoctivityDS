using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Domain.Entities;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Persistence.Context;

namespace ProDoctivityDS.Persistence.Repositories
{
    public class ActivityLogEntryRepository : BaseRepository<ActivityLogEntry>, IActivityLogEntryRepository
    {
        private readonly ProDoctivityDSDbContext _context;
        private readonly IMapper _mapper;

        public ActivityLogEntryRepository(ProDoctivityDSDbContext context, IMapper mapper) : base(context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task AddAsync(ActivityLogEntry log)
        {
            var entity = _mapper.Map<ActivityLogEntry>(log);
            _context.ActivityLogs.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ActivityLogEntry>> GetRecentAsync(int limit = 100)
        {
            var entities = await _context.ActivityLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();
            return _mapper.Map<IEnumerable<ActivityLogEntry>>(entities);
        }

        public async Task<IEnumerable<ActivityLogEntry>> GetByDocumentIdAsync(string documentId)
        {
            var entities = await _context.ActivityLogs
                .Where(l => l.DocumentId == documentId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            return _mapper.Map<IEnumerable<ActivityLogEntry>>(entities);
        }

        public async Task ClearAsync()
        {
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
            await _context.SaveChangesAsync();
        }
    }
}

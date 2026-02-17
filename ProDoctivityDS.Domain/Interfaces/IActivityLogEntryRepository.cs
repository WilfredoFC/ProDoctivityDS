using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Domain.Interfaces
{
    public interface IActivityLogEntryRepository : IBaseRepository<ActivityLogEntry>
    {
        Task<IEnumerable<ActivityLogEntry>> GetRecentAsync(int limit = 100);
        Task<IEnumerable<ActivityLogEntry>> GetByDocumentIdAsync(string documentId);
        Task ClearAsync();
    }
}

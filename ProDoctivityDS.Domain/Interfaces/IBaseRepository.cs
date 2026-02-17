namespace ProDoctivityDS.Domain.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetEntityByIdAsync(int Id);
        Task<T> SaveEntityAsync(T entity);
        Task<T?> UpdateEntityAsync(int id, T entity);
        Task<List<T>> GetAllListAsync();
        IQueryable<T> GetAllQuery();
        Task RemoveAsync(int Id);
        Task<List<T>> GetAllListWithInclude(List<string> properties);
        IQueryable<T> GetAllQueryWithInclude(List<string> properties);
    }
}
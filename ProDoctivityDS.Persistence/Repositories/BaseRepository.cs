using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Domain.Interfaces;
using ProDoctivityDS.Persistence.Context;

namespace ProDoctivityDS.Persistence.Repositories
{

    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly ProDoctivityDSDbContext _context;
        protected DbSet<TEntity> Entity { get; }

        public BaseRepository(ProDoctivityDSDbContext context)
        {
            _context = context;
            Entity = context.Set<TEntity>();
        }

        public virtual async Task<List<TEntity>> GetAllListAsync()
        {
            return await Entity.ToListAsync();
        }

        public IQueryable<TEntity> GetAllQuery()
        {
            return Entity.AsQueryable();
        }

        public virtual IQueryable<TEntity> GetAllQueryWithInclude(List<string> properties)
        {
            var query = Entity.AsQueryable();

            foreach (var property in properties)
            {
                query.Include(property);
            }

            return query;
        }

        public virtual async Task<List<TEntity>> GetAllListWithInclude(List<string> properties)
        {
            var query = Entity.AsQueryable();

            foreach (var property in properties)
            {
                query.Include(property);
            }

            return await query.ToListAsync();
        }

        public virtual async Task<TEntity?> GetEntityByIdAsync(int Id)
        {
            return await Entity.FindAsync(Id);
        }

        public virtual async Task RemoveAsync(int Id)
        {
            var entity = await Entity.FindAsync(Id);

            if (entity != null)
            {
                Entity.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TEntity> SaveEntityAsync(TEntity entity)
        {
            await Entity.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<TEntity?> UpdateEntityAsync(int id, TEntity entity)
        {
            var entry = await Entity.FindAsync(id);

            if (entry != null)
            {
                _context.Entry(entry).CurrentValues.SetValues(entity);
                await _context.SaveChangesAsync();
                return entry;
            }
            return null;
        }
    }
}

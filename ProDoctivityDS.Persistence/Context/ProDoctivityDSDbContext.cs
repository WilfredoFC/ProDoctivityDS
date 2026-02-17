using Microsoft.EntityFrameworkCore;
using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Persistence.Context
{
    public class ProDoctivityDSDbContext : DbContext
    {
        public ProDoctivityDSDbContext(DbContextOptions<ProDoctivityDSDbContext> options) : base(options)
        {
        }

        public DbSet<StoredConfiguration> StoredConfigurations { get; set; }
        public DbSet<ActivityLogEntry> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProDoctivityDSDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}

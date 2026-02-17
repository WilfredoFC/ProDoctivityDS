using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDoctivityDS.Domain.Entities;

namespace ProDoctivityDS.Persistence.EntitiesConfiguration
{
    public class ActivityLogEntryConfiguration : IEntityTypeConfiguration<ActivityLogEntry>
    {
        public void Configure(EntityTypeBuilder<ActivityLogEntry> builder)
        {
            builder.HasIndex(l => l.Timestamp)
                   .HasDatabaseName("IX_ActivityLogs_Timestamp")
                   .IsDescending();

            builder.HasIndex(l => l.DocumentId)
                   .HasDatabaseName("IX_ActivityLogs_DocumentId");

            // Configurar longitud máxima para campos de texto
            builder.Property(l => l.Level)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(l => l.Category)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(l => l.DocumentId)
                   .HasMaxLength(100);
        }
    }
}
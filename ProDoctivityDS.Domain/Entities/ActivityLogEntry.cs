using ProDoctivityDS.Domain.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProDoctivityDS.Domain.Entities
{

    [Table("ActivityLogs")]
    public class ActivityLogEntry : BaseEntity
    {
        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(20)]
        public string Level { get; set; } = "INFO";

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DocumentId { get; set; }
    }
}

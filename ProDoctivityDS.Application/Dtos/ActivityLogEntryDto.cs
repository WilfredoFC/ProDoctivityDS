using System.ComponentModel.DataAnnotations;

namespace ProDoctivityDS.Application.Dtos
{
    public class ActivityLogEntryDto
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

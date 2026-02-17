using System.ComponentModel.DataAnnotations;

namespace ProDoctivityDS.Application.Dtos
{
    public abstract class BaseDto
    {
        [Key]
        public int Id { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace ProDoctivityDS.Domain.Base
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}

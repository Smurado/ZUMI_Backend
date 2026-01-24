using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class Rolle
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Beschreibung { get; set; }
    }
}
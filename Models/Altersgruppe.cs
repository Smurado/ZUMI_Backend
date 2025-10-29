using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class Altersgruppe
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(3)]
        public string AlterMin { get; set; }

        [Required]
        [MaxLength(3)]
        public string AlterMax { get; set; }
    }
}
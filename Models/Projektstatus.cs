using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class Projektstatus
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Bezeichnung { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class Material
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        public string Beschreibung { get; set; }

        public bool Vorhanden { get; set; } = false;

        public Guid ProjektId { get; set; }
        
        [ForeignKey(nameof(ProjektId))]
        public virtual Project Projekt { get; set; }
    }
}
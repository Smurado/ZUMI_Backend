using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class Materialien
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        public string Beschreibung { get; set; }

        public bool Vorhanden { get; set; } = false;

        public virtual ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();  // Many-to-Many through ProjektMaterialien
    }
}
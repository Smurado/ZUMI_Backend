using System.ComponentModel.DataAnnotations;

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

        public virtual ICollection<Project> Projekte { get; set; } = new List<Project>();  // Many-to-Many through ProjektMaterialien
    }
}
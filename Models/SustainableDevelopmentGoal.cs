using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class SustainableDevelopmentGoal
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public int Nummer { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        public virtual ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();  // Many-to-Many through ProjektSDG
    }
}
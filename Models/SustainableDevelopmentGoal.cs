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

        public virtual ICollection<Project> Projekte { get; set; } = new List<Project>();  // Many-to-Many through ProjektSDG
    }
}
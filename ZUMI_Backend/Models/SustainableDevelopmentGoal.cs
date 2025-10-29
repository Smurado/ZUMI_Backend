using System.Collections.Generic;
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

        // Many-to-Many: EF Core handhabt through 'ProjektSDG' automatisch
        public virtual ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();
    }
}
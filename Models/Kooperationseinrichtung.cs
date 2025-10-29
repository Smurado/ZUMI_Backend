using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class Kooperationseinrichtung
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Webseite { get; set; }

        [Required]
        [MaxLength(15)]
        public string Telefonnummer { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string SocialMedia { get; set; }

        // Many-to-Many: EF Core handhabt through 'ProjektKooperationseinrichtung' automatisch
        public virtual ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();
    }
}
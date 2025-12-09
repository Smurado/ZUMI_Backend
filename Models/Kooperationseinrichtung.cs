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
        
        public string Firma { get; set; }

        [MaxLength(255)]
        public string Webseite { get; set; }

        [MaxLength(15)]
        public string Telefonnummer { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [MaxLength(255)]
        public string SocialMedia { get; set; }

        public virtual ICollection<Project> Projekte { get; set; } = new List<Project>();  // Many-to-Many through ProjektKooperationseinrichtung
    }
}
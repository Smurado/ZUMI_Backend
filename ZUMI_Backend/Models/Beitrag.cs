using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models
{
    public class Beitrag
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Beschreibung { get; set; }

        public virtual ICollection<Person> Personen { get; set; } = new List<Person>();  // Many-to-Many through PersonBeitrag
    }
}
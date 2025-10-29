using System.Collections.Generic;
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

        // Many-to-Many: EF Core handhabt through 'PersonBeitrag' automatisch
        public virtual ICollection<Person> Personen { get; set; } = new List<Person>();
    }
}
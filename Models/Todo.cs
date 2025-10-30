using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class Todo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(255)]
        public string Titel { get; set; }

        public string Beschreibung { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "offen";  // Choices: offen, in_bearbeitung, abgeschlossen

        public Guid ProjektId { get; set; }
        [ForeignKey(nameof(ProjektId))]
        public virtual Projekt Projekt { get; set; }
    }
}
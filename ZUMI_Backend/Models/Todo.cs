using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZUMI_Backend.Models.Enums;

namespace ZUMI_Backend.Models
{
    public class Todo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(255)]
        public string Titel { get; set; }

        public string Beschreibung { get; set; }
        
        public TodoStatus Status { get; set; } = 0;  // Choices: offen, in_bearbeitung, abgeschlossen

        public Guid ProjectId { get; set; }
        
        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }
    }
}
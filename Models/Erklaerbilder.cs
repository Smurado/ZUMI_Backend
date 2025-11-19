using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class Erklaerbild
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProjektId { get; set; }
        [ForeignKey(nameof(ProjektId))]
        public virtual Project Project { get; set; }

        public string url { get; set; }  // URL/Path zu ImageField
    }
}
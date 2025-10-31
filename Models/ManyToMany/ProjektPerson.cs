using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class ProjektPerson
    {
        public Guid PersonId { get; set; }
        public Guid ProjektId { get; set; }

        // Neu: Attribute für Liked und Mitmacht
        public bool IsLiked { get; set; } = false;
        public bool IsParticipating { get; set; } = false;  // "Mitmacht"

        // Navigation Properties (optional, für Queries)
        [ForeignKey(nameof(PersonId))]
        public virtual Person Person { get; set; }

        [ForeignKey(nameof(ProjektId))]
        public virtual Projekt Projekt { get; set; }
    }
}


using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class ProjektPerson
    {
        public Guid PersonId { get; set; }
        public Guid ProjektId { get; set; }

        //
        public bool IsLiked { get; set; } = false;
        public bool IsOwner { get; set; } = false;
        public bool IsParticipating { get; set; } = false; 

        // Navigation Properties (optional, für Queries)
        [ForeignKey(nameof(PersonId))]
        public virtual Person Person { get; set; }

        [ForeignKey(nameof(ProjektId))]
        public virtual Project Project { get; set; }
    }
}


namespace ZUMI_Backend.Models.ManyToMany;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public class ProjektPerson
{
    public Guid PersonId { get; set; }
    public Guid ProjektId { get; set; }

    public bool IsOwner { get; set; } = false; // Super-Admin
    public bool IsLiked { get; set; } = false; // "Fan"-Status
    
    // Eine Person kann mehrere Rollen haben
    public virtual ICollection<ProjektPersonRole> Roles { get; set; } = new List<ProjektPersonRole>();

    // Navigation Properties
    [ForeignKey(nameof(PersonId))]
    public virtual Person Person { get; set; }

    [ForeignKey(nameof(ProjektId))]
    public virtual Project Project { get; set; }
}



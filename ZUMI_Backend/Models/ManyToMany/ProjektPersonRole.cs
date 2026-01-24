namespace ZUMI_Backend.Models.ManyToMany;
using System.ComponentModel.DataAnnotations.Schema;

// Die Verbindungstabelle: Welche Rollen hat eine Person in einem Projekt?
public class ProjektPersonRole
{
    // 1. Verweis auf den Teilnehmer (ProjektPerson)
    public Guid PersonId { get; set; }
    public Guid ProjektId { get; set; }
    
    [ForeignKey("PersonId, ProjektId")]
    public virtual ProjektPerson ProjektPerson { get; set; }

    // 2. Verweis auf die Rolle
    public Guid ProjectRoleId { get; set; }
    
    [ForeignKey(nameof(ProjectRoleId))]
    public virtual ProjectRole ProjectRole { get; set; }
}

namespace ZUMI_Backend.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Enums;

public class ProjectRole
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } // z.B. "Lehrer"

    public ProjectPermissions Permissions { get; set; }

    /// <summary>
    /// Wenn true, kann diese Rolle vom User nicht gelöscht oder umbenannt werden (z.B. "Mitglied").
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    public Guid ProjectId { get; set; }
    
    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; }
}

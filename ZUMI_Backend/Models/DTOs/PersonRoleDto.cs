namespace ZUMI_Backend.Models.DTOs;

public class PersonRoleDto
{
    public Guid PersonId { get; set; }
        
    // Status Flags
    public bool IsLiked { get; set; } = false;
    public bool IsOwner { get; set; } = false;
        
    /// <summary>
    /// True, wenn der User Owner ist ODER mindestens eine Rolle hat.
    /// </summary>
    public bool IsParticipating { get; set; } = false;
        
    // Person Details
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; }
        
    // Avatar (Profilbild), nützlich für die Member-Liste im Frontend
    public string? Avatar { get; set; }

    // Liste der Rollen vom Benutzer..
    public List<ProjectRoleDto> Roles { get; set; } = new List<ProjectRoleDto>();
    
    // Telete Person from Project
    public bool Delete { get; set; } = false;
}

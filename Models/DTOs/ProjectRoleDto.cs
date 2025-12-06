namespace ZUMI_Backend.Models.DTOs;

public class ProjectRoleDto
{
    public Guid ProjektId { get; set; }
    public bool IsLiked { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public bool IsParticipating { get; set; } = false;
    
}
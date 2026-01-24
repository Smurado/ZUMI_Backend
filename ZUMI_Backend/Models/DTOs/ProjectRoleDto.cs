namespace ZUMI_Backend.Models.DTOs;

public class ProjectRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Permissions { get; set; } // Die Bitmaske als Zahl
    public bool IsSystemRole { get; set; }
}
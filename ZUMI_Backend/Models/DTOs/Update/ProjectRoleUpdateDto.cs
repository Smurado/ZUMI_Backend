namespace ZUMI_Backend.Models.DTOs;

public class ProjectRoleUpdateDto
{
    // GUID nullable, weil bei neuen Rollen leer
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Das Bitmask-Int vom Frontend
    public int? PermissionPoints { get; set; }

    // Das Flag zum Löschen
    public bool Delete { get; set; } = false;
}
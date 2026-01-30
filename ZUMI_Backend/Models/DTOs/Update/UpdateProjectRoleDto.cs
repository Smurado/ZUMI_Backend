namespace ZUMI_Backend.Models.DTOs;

public class UpdateProjectRoleDto
{
    // GUID nullable, weil bei neuen Rollen leer
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Das Bitmask-Int vom Frontend
    public int? Permissions { get; set; }

    // Das Flag zum Löschen
    public bool Delete { get; set; } = false;
}
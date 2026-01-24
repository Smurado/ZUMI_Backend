namespace ZUMI_Backend.Models.DTOs;

public class MaterialUpdateDto
{
    public Guid? Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Beschreibung { get; set; } = string.Empty;
    
    public bool Vorhanden { get; set; } = false;
    
    public Guid ProjektId { get; set; }
    
    public bool Delete { get; set; } = false;
}
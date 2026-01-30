namespace ZUMI_Backend.Models.DTOs;

public class MaterialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Beschreibung { get; set; }
    public bool Vorhanden { get; set; }
    
    public Guid? ProjectId { get; set; }
    
    public string? ProjectTitle { get; set; }
}
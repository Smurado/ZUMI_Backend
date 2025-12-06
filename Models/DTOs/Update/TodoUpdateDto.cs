namespace ZUMI_Backend.Models.DTOs;
using Enums;

public class TodoUpdateDto
{
    public Guid? Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    
    public string Beschreibung { get; set; } = string.Empty;
    
    public TodoStatus Status { get; set; }
    
    public bool Delete { get; set; } = false; 
}

using ZUMI_Backend.Models.Enums;

namespace ZUMI_Backend.Models.DTOs;

public class TodoDto
{
    public Guid Id { get; set; }
    
    public string Title { get; set; } 
    
    public string Beschreibung {get; set;}
    
    public Guid ProjectId { get; set; }
    
    public TodoStatus Status { get; set; }
    
    public string? ProjectTitle {get; set;}
}
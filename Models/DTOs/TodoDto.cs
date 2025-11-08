namespace ZUMI_Backend.Models.DTOs;

public class TodoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } 
    public Guid ProjektId { get; set; }
    // Weitere Felder
}
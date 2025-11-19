namespace ZUMI_Backend.Models.DTOs;

public class TodoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } 
    public Guid ProjectId { get; set; }
}
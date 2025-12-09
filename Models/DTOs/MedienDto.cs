namespace ZUMI_Backend.Models.DTOs;
using Enums;

public class MedienDto
{
    public Guid Id { get; set; }
    public string Url { get; set; }  
    public Guid ProjektId { get; set; }
    
    public MediaType MediaType { get; set; }
    
    public MediaStatus Status { get; set; }
    
    public string OriginalFileName { get; set; }
}
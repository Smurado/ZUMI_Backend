namespace ZUMI_Backend.Models.DTOs;

public class KooperationseinrichtungUpdateDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    
    public string Firma { get; set; }
    public string Email { get; set; }
    public string Webseite { get; set; } = string.Empty;
    public string SocialMedia { get; set; } = string.Empty;
    public string Telefonnummer { get; set; } = string.Empty;
    public bool Delete { get; set; } = false; 
}

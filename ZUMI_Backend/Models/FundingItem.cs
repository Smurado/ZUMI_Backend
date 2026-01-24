namespace ZUMI_Backend.Models;

public class FundingItem
{
    public Guid Id { get; set; }
    
    // "Was ist es?"
    public string Titel { get; set; } = string.Empty; 
    
    // "Beschreibung"
    public string Beschreibung { get; set; } = string.Empty;
    
    // "Wie viel Geld braucht man?"
    public decimal BenoetigterBetrag { get; set; }
    
    // Optional: Damit wir wissen, wie viel schon da ist (für den Ladebalken)
    public decimal BereitsGesammelt { get; set; } = 0;

    // Verknüpfung zum Projekt
    public Guid ProjectId { get; set; }
    // JsonIgnore verhindert Zyklen beim Serialisieren
    [System.Text.Json.Serialization.JsonIgnore] 
    public Project? Project { get; set; }
}
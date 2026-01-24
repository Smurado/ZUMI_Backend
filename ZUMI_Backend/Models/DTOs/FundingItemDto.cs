namespace ZUMI_Backend.Models.DTOs;

public class FundingItemDto
{
    public Guid Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Beschreibung { get; set; } = string.Empty;
    
    // Wir senden reine Zahlen, das Frontend formatiert dann (z.B. "300 €")
    public decimal BenoetigterBetrag { get; set; }
    public decimal BereitsGesammelt { get; set; }
    
    // Optional: Ein berechneter Wert für den Ladebalken (0.0 bis 1.0)
    public double ProzentErreicht => BenoetigterBetrag > 0 
        ? (double)(BereitsGesammelt / BenoetigterBetrag) 
        : 0;
}
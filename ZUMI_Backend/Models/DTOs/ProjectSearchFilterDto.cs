namespace ZUMI_Backend.Models.DTOs;

using Enums;

public class ProjectSearchFilter
{
    // 1 = In Vorbereitung, 2 = In Umsetzung, 3 = Abgeschlossen
    public int? Status { get; set; }

    // Steuert, welche spezifischen Daten geladen werden: 
    // "todos", "materials", "financing", "locations", "discovery"
    public ProjectFilterCategory Category { get; set; }

    // Pagination: Startet bei 0
    public int Page { get; set; } = 0;

    // Festgelegtes Limit von 10 Projekten
    public int Limit { get; set; } = 10;

    // Optionale Zusatzfilter für die Suche
    public string? SearchTerm { get; set; }
    public int? SdgId { get; set; }
}
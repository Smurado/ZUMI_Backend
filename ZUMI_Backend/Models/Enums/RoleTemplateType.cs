namespace ZUMI_Backend.Models.Enums;

public enum RoleTemplateType
{
    Standard = 0,      // Erstellt: Owner, Mitglied, Liker
    Schulklasse = 1,   // Erstellt zusätzlich: Lehrer, Schüler
    Verein = 2,        // Erstellt zusätzlich: Kassenwart, Vorstand
    
}

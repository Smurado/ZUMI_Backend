namespace ZUMI_Backend.Models.Enums;

using System.ComponentModel.DataAnnotations;

public enum RoleTemplateType
{
    [Display(Name = "Standard: Projektleitung und Mitglied")]
    Standard = 0,

    [Display(Name = "Schulklasse: Fügt Lehrer (Admin) und Schüler hinzu")]
    Schulklasse = 1,

    [Display(Name = "Verein: Fügt Vorstand und Kassenwart hinzu")]
    Verein = 2
}

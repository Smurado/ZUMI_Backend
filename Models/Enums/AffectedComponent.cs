// Enums/AffectedComponent.cs
using System.ComponentModel.DataAnnotations;

namespace ZUMI_Backend.Models.Enums;

public enum FeedbackAffectedComponent
{
    [Display(Name = "Login")]
    None = 0,

    [Display(Name = "Registrierung")]
    Registrierung = 1,

    [Display(Name = "Passwort Vergessen")]
    PasswortVergessen = 2,

    [Display(Name = "Gast Anmeldung")]
    GastAnmeldung = 3,

    [Display(Name = "Profil")]
    Profil = 4,

    [Display(Name = "Einstellungen anpassen")]
    EinstellungenAnpassen = 5,

    [Display(Name = "Startseite")]
    Startseite = 6,

    [Display(Name = "Projekt erstellen")]
    ProjektErstellen = 7,

    [Display(Name = "Projekt anzeigen")]
    ProjektAnzeigen = 8,

    [Display(Name = "Projekt anpassen")]
    ProjektAnpassen = 9,

    [Display(Name = "Angemeldet bleiben")]
    AngemeldetBleiben = 10,
    
    [Display(Name = "Abmelden")]
    Abmelden = 11,

    [Display(Name = "Sonstiges")]
    Sonstiges = 99
}
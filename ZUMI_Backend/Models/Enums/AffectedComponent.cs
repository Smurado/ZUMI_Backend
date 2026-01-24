namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public enum FeedbackAffectedComponent
{
    [Display(Name = "Login/Registrierung/Abmelden")]
    Authentication = 1,

    [Display(Name = "Startseite")]
    Home = 2,

    [Display(Name = "Projektideenseite")]
    ProjectIdeas = 3,

    [Display(Name = "Projektdetails")]
    ProjectDetails = 4,

    [Display(Name = "Profilseite")]
    Profile = 5,

    [Display(Name = "Projekt erstellen und bearbeiten")]
    ProjectManagement = 6,

    [Display(Name = "Download der App")]
    AppDownload = 7,

    [Display(Name = "Sonstiges")]
    Other = 99
}
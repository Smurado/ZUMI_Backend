namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;
public enum FeedbackCategory
{
    [Display(Name = "Technischer Fehler")]
    TechnicalError = 1,
    
    [Display(Name = "Wünsche")]
    FeatureRequest = 2,

    [Display(Name = "Hilfe")]
    Support = 3,

    [Display(Name = "Geschwindigkeit")]
    Performance = 4,

    [Display(Name = "Design")]
    Design = 5,

    [Display(Name = "Lob")]
    Praise = 6,

    [Display(Name = "Sonstiges")]
    Other = 99
}
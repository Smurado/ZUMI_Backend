namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;
public enum FeedbackCategory
{
    [Display(Name = "Allgemein")]
    General = 0,

    [Display(Name = "Fehler / Bug")]
    Bug = 1,

    [Display(Name = "Feature-Wunsch")]
    FeatureRequest = 2,

    [Display(Name = "Support-Anfrage")]
    Support = 3,

    [Display(Name = "Performance")]
    Performance = 4,

    [Display(Name = "Design / UI")]
    Design = 5,

    [Display(Name = "Sonstiges")]
    Other = 99
}
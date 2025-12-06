namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public enum TodoStatus
{
    [Display(Name ="Offen")]
    Offen = 0,
    
    [Display(Name ="In  Bearbeitung")]
    InBearbeitung = 1,
    
    [Display(Name ="Erledigt")]
    Erledigt = 2,
}
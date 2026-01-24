namespace ZUMI_Backend.Models.Enums;

using System.ComponentModel.DataAnnotations;

public enum ProjectFilterCategory
{
    [Display(Name = "Entdecken")]
    Discovery = 0, // Standard

    [Display(Name = "Neu erstellt")]
    New = 1,

    [Display(Name = "Todos")]
    Todos = 2,

    [Display(Name = "Materialien")]
    Materials = 3,

    [Display(Name = "Finanzierung")]
    Financing = 4,

    [Display(Name = "Fantastische Arbeit")]
    Random = 5,

    [Display(Name = "Standorte")]
    Locations = 6
}
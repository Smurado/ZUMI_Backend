namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public enum Altersgruppe
{
    /// <summary>
    /// Kinder (ca. 6-12 Jahre)
    /// </summary>
    [Display(Name = "Kinder (6-12 Jahre)")]
    Kind = 0,

    /// <summary>
    /// Jugendliche (ca. 13-17 Jahre)
    /// </summary>
    [Display(Name = "Jugendliche (13-17 Jahre)")]
    Jugend = 1,

    /// <summary>
    /// Junge Erwachsene (ca. 18-30 Jahre)
    /// </summary>
    [Display(Name = "Junge Erwachsene (18-30 Jahre)")]
    JungeErwachsene = 2,

    /// <summary>
    /// Erwachsene (ca. 31-60 Jahre)
    /// </summary>
    [Display(Name = "Erwachsene (31-60 Jahre)")]
    Erwachsene = 3,

    /// <summary>
    /// Senioren (ca. 61+ Jahre)
    /// </summary>
    [Display(Name = "Senioren (61+ Jahre)")]
    Senioren = 4
}
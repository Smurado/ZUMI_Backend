namespace ZUMI_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public enum ProjektStatus
{
    /// <summary>
    /// Projekt ist geplant, aber noch nicht gestartet.
    /// </summary>
    [Display(Name = "Geplant")]
    Geplant = 0,

    /// <summary>
    /// Projekt befindet sich in der Vorbereitungsphase.
    /// </summary>
    [Display(Name = "In Vorbereitung")]
    InVorbereitung = 1,

    /// <summary>
    /// Projekt wird aktiv umgesetzt.
    /// </summary>
    [Display(Name = "In Umsetzung")]
    InUmsetzung = 2,

    /// <summary>
    /// Projekt ist erfolgreich abgeschlossen.
    /// </summary>
    [Display(Name = "Abgeschlossen")]
    Abgeschlossen = 3,

    /// <summary>
    /// Projekt ist archiviert und nicht mehr aktiv.
    /// </summary>
    [Display(Name = "Archiviert")]
    Archiviert = 4
}
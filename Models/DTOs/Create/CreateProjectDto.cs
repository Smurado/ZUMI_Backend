namespace ZUMI_Backend.Models.DTOs;

using System.ComponentModel.DataAnnotations;

public class CreateProjectDto
{
    [Required, MaxLength(50)]
    public string Kurztitel { get; set; } = null!;

    public string? Kurzbeschreibung { get; set; }

    public string? Titelbild { get; set; }  // Optional beim Create

    [Required]
    public string Beschreibung { get; set; } = null!;

    [Required, MaxLength(255)]
    public string Vorbereitungszeitraum { get; set; } = null!;

    [Required, MaxLength(255)]
    public string Umsetzungszeitraum { get; set; } = null!;

    public string? StandortLink { get; set; }

    [MaxLength(255)]
    public string? Adresse { get; set; }

    [Required, MaxLength(5)]
    public string Plz { get; set; } = null!;

    [MaxLength(255)]
    public string? Spendeninformationen { get; set; }

    public string? WeitereInfos { get; set; }

    public double? GesamtBudget { get; set; }  // Optional, default 0

    public double? SpentBudget { get; set; }  // Optional, default 0

    public string? SpendenLink { get; set; }

    public string? Finance { get; set; }
}
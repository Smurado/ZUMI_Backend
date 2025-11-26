namespace ZUMI_Backend.Models.DTOs;

using System.ComponentModel.DataAnnotations;
using Enums;

public class UpdateProjectDto
{
    [MaxLength(50)] public string? Kurztitel { get; set; }

    public string? Kurzbeschreibung { get; set; }

    public string? Titelbild { get; set; }

    public string? Beschreibung { get; set; }

    [MaxLength(255)] public string? Vorbereitungszeitraum { get; set; }

    [MaxLength(255)] public string? Umsetzungszeitraum { get; set; }

    public string? StandortLink { get; set; }

    [MaxLength(255)] public string? Adresse { get; set; }

    [MaxLength(5)] public string? Plz { get; set; }

    [MaxLength(255)] public string? Spendeninformationen { get; set; }

    public string? WeitereInfos { get; set; }

    public string? LetztesUpdate { get; set; } // Optional, auto-update wenn null

    public double? GesamtBudget { get; set; }

    public double? SpentBudget { get; set; }

    public string? SpendenLink { get; set; }

    public string? Finance { get; set; }

    public Projektstatus? Projektstatus { get; set; } // Enum für Status-Update

    public List<int>? SdgValues { get; set; }
}
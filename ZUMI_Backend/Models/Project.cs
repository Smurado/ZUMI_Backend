namespace ZUMI_Backend.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ManyToMany;
using Enums;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Kurztitel { get; set; }
    
    public string? Kurzbeschreibung { get; set; }

    [Required]
    public string Beschreibung { get; set; }
    
    [MaxLength(255)]
    public string? Vorbereitungszeitraum { get; set; }
    
    [MaxLength(255)]
    public string? Umsetzungszeitraum { get; set; }

    public string? StandortLink { get; set; }  // URLField

    [MaxLength(255)]
    public string? Adresse { get; set; }
    
    [MaxLength(5)]
    public string? Plz { get; set; }

    [MaxLength(255)]
    public string? Spendeninformationen { get; set; }

    public string? WeitereInfos { get; set; }

    public string? LetztesUpdate { get; set; }  // Als string, da TextField; ggf. zu DateTime ändern, wenn timestamp

    public double? GesamtBudget { get; set; }

    public double? SpentBudget { get; set; }
    
    public string? Finance { get; set; }
    
    public string SpendenLink { get; set; }
    
    public virtual ProjektStatus ProjektStatus { get; set; }
    
    public virtual ICollection<ProjektPerson> Personen { get; set; } = new List<ProjektPerson>();  
    
    // Die definierten Rollen für dieses Projekt (z.B. "Lehrer", "Schüler")
    // Das ist wichtig, damit EF Core beim Löschen des Projekts auch die Rollen-Definitionen löscht (Cascade)
    public virtual ICollection<ProjectRole> Roles { get; set; } = new List<ProjectRole>();
    
    [NotMapped]  // Optional: Für Enum-Helper
    public List<Sdg> Sdgs => SdgValues.Select(v => (Sdg)v).ToList();
    
    public List<FundingItem> FundingItems { get; set; } = new();

    /// <summary>
    /// SDG-Werte als Liste von ints (z. B. [1, 3, 7]).
    /// </summary>
    public List<int> SdgValues { get; set; } = new List<int>();
    
    public virtual ICollection<Kooperationseinrichtung> Kooperationseinrichtungen{ get; set; } = new List<Kooperationseinrichtung>();  // Through ProjektKooperationseinrichtung
    public virtual ICollection<Material> Materialien { get; set; } = new List<Material>();  // Through ProjektMaterialien
    public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
    public virtual ICollection<Medien> Medien { get; set; } = new List<Medien>();
}

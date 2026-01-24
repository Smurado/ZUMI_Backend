namespace ZUMI_Backend.Models;

using System.ComponentModel.DataAnnotations;
using ManyToMany;
using Enums;

public class Person
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]  // Passwort-Feld (wird gehasht gespeichert)
    public string Password { get; set; } 
    
    [MaxLength(5)]
    public string? Plz { get; set; }

    [MaxLength(50)]
    public string? Land { get; set; }

    [MaxLength(255)]
    public string? Ort { get; set; }
    
    [MaxLength(255)]
    public string? Sprache { get; set; }

    public string? Interessen { get; set; }

    public string? Staerken { get; set; }

    public string? Avatar { get; set; }  // Path to FilePathField
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public virtual Altersgruppe Altersgruppe { get; set; }

    // Many-to-Many
    public virtual ICollection<ProjektPerson> Projekte { get; set; } = new List<ProjektPerson>();  // Through ProjektPerson
    public virtual ICollection<Beitrag> Beitraege { get; set; } = new List<Beitrag>();  // Through PersonBeitrag
}

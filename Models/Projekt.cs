using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class Projekt
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Kurztitel { get; set; }
        
        public string? Kurzbeschreibung { get; set; }

        public string Titelbild { get; set; }  // URL/Path zu ImageField

        [Required]
        public string Beschreibung { get; set; }

        [Required]
        [MaxLength(255)]
        public string Vorbereitungszeitraum { get; set; }

        [Required]
        [MaxLength(255)]
        public string Umsetzungszeitraum { get; set; }

        public string? StandortLink { get; set; }  // URLField

        [MaxLength(255)]
        public string Adresse { get; set; }

        [Required]
        [MaxLength(5)]
        public string Plz { get; set; }

        [MaxLength(255)]
        public string? Spendeninformationen { get; set; }

        public string? WeitereInfos { get; set; }

        public string? LetztesUpdate { get; set; }  // Als string, da TextField; ggf. zu DateTime ändern, wenn timestamp

        // Foreign Key
        public Guid ProjektstatusId { get; set; }
        [ForeignKey(nameof(ProjektstatusId))]
        public virtual Projektstatus Projektstatus { get; set; }

        // Many-to-Many
        public virtual ICollection<ProjektPerson> Personen { get; set; } = new List<ProjektPerson>();  // Through ProjektPerson
        public virtual ICollection<SustainableDevelopmentGoal> Sdgs { get; set; } = new List<SustainableDevelopmentGoal>();  // Through ProjektSDG
        public virtual ICollection<Kooperationseinrichtung> Kooperationseinrichtungen{ get; set; } = new List<Kooperationseinrichtung>();  // Through ProjektKooperationseinrichtung
        public virtual ICollection<Materialien> Materialien { get; set; } = new List<Materialien>();  // Through ProjektMaterialien
        public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
    }
}
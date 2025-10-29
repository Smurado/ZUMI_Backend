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
        public string Name { get; set; }

        [Required]
        public string Beschreibung { get; set; }

        public string Titelbild { get; set; }  // Pfad/URL

        public string Bild { get; set; }

        public string Erklaerbild { get; set; }

        [Required]
        [MaxLength(5)]
        public string Plz { get; set; }

        [Required]
        [MaxLength(255)]
        public string Land { get; set; }

        [Required]
        [MaxLength(255)]
        public string Ort { get; set; }

        [Required]
        [MaxLength(255)]
        public string Vorbereitungszeit { get; set; }

        [Required]
        [MaxLength(255)]
        public string Umsetzungszeit { get; set; }

        [Required]
        public DateTime Beginn { get; set; }

        [Required]
        public DateTime Ende { get; set; }

        // Foreign Key
        public Guid ProjektstatusId { get; set; }
        [ForeignKey(nameof(ProjektstatusId))]
        public virtual Projektstatus Projektstatus { get; set; }

        // Many-to-Many (reverse)
        public virtual ICollection<Person> Personen { get; set; } = new List<Person>();
        public virtual ICollection<SustainableDevelopmentGoal> Sdgs { get; set; } = new List<SustainableDevelopmentGoal>();
        public virtual ICollection<Kooperationseinrichtung> Kooperationseinrichtungen { get; set; } = new List<Kooperationseinrichtung>();
    }
}
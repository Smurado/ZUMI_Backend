using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZUMI_Backend.Models
{
    public class Person
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        public string Email { get; set; }  // Als Username verwendet

        [Required]
        [MaxLength(5)]
        public string Plz { get; set; }

        [MaxLength(50)]
        public string Land { get; set; }

        [MaxLength(255)]
        public string Ort { get; set; }

        [Required]
        [MaxLength(255)]
        public string Sprache { get; set; }

        public string Interessen { get; set; }

        public string Staerken { get; set; }

        public string Avatar { get; set; }  // Pfad/URL zum Avatar

        [Required]
        public string FirstName { get; set; }  // Aus AbstractUser (first_name)

        [Required]
        public string LastName { get; set; }   // Aus AbstractUser (last_name)

        // Foreign Keys
        public Guid? AltersgruppeId { get; set; }
        [ForeignKey(nameof(AltersgruppeId))]
        public virtual Altersgruppe Altersgruppe { get; set; }

        public Guid RolleId { get; set; }
        [ForeignKey(nameof(RolleId))]
        public virtual Rolle Rolle { get; set; }

        // Many-to-Many
        public virtual ICollection<Projekt> Projekte { get; set; } = new List<Projekt>();  // Through 'ProjektPerson'
        public virtual ICollection<Beitrag> Beitraege { get; set; } = new List<Beitrag>();  // Through 'PersonBeitrag'
    }
}
using System;
using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Models;

namespace ZUMI_Backend.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (context.Projekte.Any()) return;  // Seed nur, wenn leer

                // Beispiel-Daten
                var status = new Projektstatus { Bezeichnung = "Aktiv" };
                context.Projektstatuses.Add(status);

                var rolle = new Rolle { Beschreibung = "Admin" };
                context.Rollen.Add(rolle);

                var sdg = new SustainableDevelopmentGoal { Nummer = 13, Name = "Klimaschutz" };
                context.SustainableDevelopmentGoals.Add(sdg);

                var altersgruppe = new Altersgruppe { AlterMin = "18", AlterMax = "30" };
                context.Altersgruppen.Add(altersgruppe);

                var person = new Person
                {
                    Email = "test@testung.de",
                    Password = BCrypt.Net.BCrypt.HashPassword("test"),
                    FirstName = "Max",
                    LastName = "Mustermann",
                    Plz = "12345",
                    Sprache = "Deutsch",
                    Rolle = rolle,
                    Altersgruppe = altersgruppe
                };
                context.Persons.Add(person);

                var projekt = new Project
                {
                    Kurztitel = "TestProjekt",
                    Beschreibung = "Ein Beispiel-Projekt",
                    Plz = "12345",
                    Adresse = "TestAdresse",
                    Vorbereitungszeitraum = "1 Monat",
                    Umsetzungszeitraum = "3 Monate",
                    Projektstatus = status,
                    Titelbild = "test.jpg",
                    Kooperationseinrichtungen = new List<Kooperationseinrichtung>{
                        new()
                        {
                            Name = "Test",
                            Email = "test",
                            SocialMedia = "test",
                            Telefonnummer = "test",
                            Webseite = "test"
                        }
                    }
                };
                projekt.Sdgs.Add(sdg);
                projekt.Personen.Add(new ProjektPerson
                {
                    Person = person,
                    Project = projekt
                });
                context.Projekte.Add(projekt);

                context.SaveChanges();
            }
        }
    }
}
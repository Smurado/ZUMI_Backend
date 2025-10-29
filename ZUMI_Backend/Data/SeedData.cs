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
                    Email = "test@example.com",
                    FirstName = "Max",
                    LastName = "Mustermann",
                    Plz = "12345",
                    Sprache = "Deutsch",
                    Rolle = rolle,
                    Altersgruppe = altersgruppe
                };
                context.Persons.Add(person);

                var projekt = new Projekt
                {
                    Name = "TestProjekt",
                    Beschreibung = "Ein Beispiel-Projekt",
                    Plz = "12345",
                    Land = "Deutschland",
                    Ort = "Berlin",
                    Vorbereitungszeit = "1 Monat",
                    Umsetzungszeit = "3 Monate",
                    Beginn = DateTime.UtcNow,
                    Ende = DateTime.UtcNow.AddMonths(3),
                    Projektstatus = status,
                    Titelbild = "test.jpg",
                    Bild = "test.jpg",
                    Erklaerbild = "test.jpg",
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
                projekt.Personen.Add(person);
                context.Projekte.Add(projekt);

                context.SaveChanges();
            }
        }
    }
}
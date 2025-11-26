using ZUMI_Backend.Endpoints;

namespace ZUMI_Backend.Data
{
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Models.Enums;
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context =
                   new ApplicationDbContext(
                       serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (context.Projekte.Any()) return; // Seed nur, wenn leer

                var rolle = new Rolle { Beschreibung = "Admin" };
                context.Rollen.Add(rolle);

                var person = new Person
                {
                    Email = "test@testung.de",
                    Password = BCrypt.Net.BCrypt.HashPassword("test"),
                    FirstName = "Max",
                    LastName = "Mustermann",
                    Plz = "12345",
                    Sprache = "Deutsch",
                    Altersgruppe = Altersgruppe.Erwachsene
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
                    Projektstatus = Projektstatus.InVorbereitung,
                    Titelbild = "test.jpg",
                    Kooperationseinrichtungen = new List<Kooperationseinrichtung>
                    {
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
                projekt.Sdgs.AddRange(Sdg.AffordableAndCleanEnergy, Sdg.CleanWaterAndSanitation, Sdg.GenderEquality)

            ;
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
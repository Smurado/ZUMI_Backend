using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Models;

namespace ZUMI_Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Projekt> Projekte { get; set; }
        public DbSet<Projektstatus> Projektstatuses { get; set; }
        public DbSet<Rolle> Rollen { get; set; }
        public DbSet<SustainableDevelopmentGoal> SustainableDevelopmentGoals { get; set; }
        public DbSet<Altersgruppe> Altersgruppen { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Beitrag> Beitraege { get; set; }
        public DbSet<Kooperationseinrichtung> Kooperationseinrichtungen { get; set; }
        
        // Constructor for Dependency Injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Projekt <-> ustainableDevelopmentGoal
            modelBuilder.Entity<Projekt>()
                .HasMany(p => p.Sdgs)
                .WithMany(s => s.Projekte)
                .UsingEntity(j => j.ToTable("ProjektSDG"));
            
            // Projekt <-> Person (through ProjektPerson)
            modelBuilder.Entity<Projekt>()
                .HasMany(p => p.Personen)
                .WithMany(pe => pe.Projekte)
                .UsingEntity(j => j.ToTable("ProjektPerson"));
            
            // Projekt <-> Kooperationseinrichtung (through ProjektKooperationseinrichtung)
            modelBuilder.Entity<Projekt>()
                .HasMany(p => p.Kooperationseinrichtungen)
                .WithMany(k => k.Projekte)
                .UsingEntity(j => j.ToTable("ProjektKooperationseinrichtung"));

            // Beitrag <-> Person (through PersonBeitrag)
            modelBuilder.Entity<Beitrag>()
                .HasMany(b => b.Personen)
                .WithMany(pe => pe.Beitraege)
                .UsingEntity(j => j.ToTable("PersonBeitrag"));
            
        }
    }
}
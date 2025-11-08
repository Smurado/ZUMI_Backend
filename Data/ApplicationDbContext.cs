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
        public DbSet<Todo> Todos { get; set; }
        
        public DbSet<ProjektPerson> ProjektPersons { get; set; }
        public DbSet<Erklaerbild> Erklaerbilder { get; set; }
        
        public DbSet<Materialien> Materialien { get; set; }
        
        // Constructor for Dependency Injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<OutstandingToken> OutstandingTokens { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Projekt <-> ustainableDevelopmentGoal
            modelBuilder.Entity<Projekt>()
                .HasMany(p => p.Sdgs)
                .WithMany(s => s.Projekte)
                .UsingEntity(j => j.ToTable("ProjektSDG"));
            
            modelBuilder.Entity<Projekt>()
                .HasMany(p => p.Todos);
            
            // Projekt <-> Kooperationseinrichtung (through ProjektKooperationseinrichtung)
            modelBuilder.Entity<Projekt>()
                .HasMany(p => p.Kooperationseinrichtungen)
                .WithMany(k => k.Projekte)
                .UsingEntity(j => j.ToTable("ProjektKooperationseinrichtung"));
            
            modelBuilder.Entity<Projekt>()
                .HasMany(p =>p.Materialien)
                .WithMany(m => m.Projekte)
                .UsingEntity(j => j.ToTable("ProjektMaterialien"));
            
            // Konfiguriere die Junction-Entity (ersetzt alte UsingEntity)
            modelBuilder.Entity<ProjektPerson>()
                .HasKey(pp => new { pp.PersonId, pp.ProjektId });  // Composite Key

            modelBuilder.Entity<ProjektPerson>()
                .HasOne(pp => pp.Person)
                .WithMany(p => p.Projekte)
                .HasForeignKey(pp => pp.PersonId);

            modelBuilder.Entity<ProjektPerson>()
                .HasOne(pp => pp.Projekt)
                .WithMany(pr => pr.Personen)
                .HasForeignKey(pp => pp.ProjektId);

            // Beitrag <-> Person (through PersonBeitrag)
            modelBuilder.Entity<Beitrag>()
                .HasMany(b => b.Personen)
                .WithMany(pe => pe.Beitraege)
                .UsingEntity(j => j.ToTable("PersonBeitrag"));
            
            modelBuilder.Entity<OutstandingToken>()
                .HasOne(ot => ot.User)
                .WithMany()
                .HasForeignKey(ot => ot.UserId);

            modelBuilder.Entity<BlacklistedToken>()
                .HasOne(bt => bt.Token)
                .WithMany()
                .HasForeignKey(bt => bt.TokenId);
            
            
        }
    }
}
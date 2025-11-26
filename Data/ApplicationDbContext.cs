using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ZUMI_Backend.Models;

namespace ZUMI_Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Project> Projekte { get; set; }
        //public DbSet<Projektstatus> Projektstatuses { get; set; }
        public DbSet<Rolle> Rollen { get; set; }
        //public DbSet<SustainableDevelopmentGoal> SustainableDevelopmentGoals { get; set; }
        //public DbSet<Altersgruppe> Altersgruppen { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Beitrag> Beitraege { get; set; }
        public DbSet<Kooperationseinrichtung> Kooperationseinrichtungen { get; set; }
        public DbSet<Todo> Todos { get; set; }
        
        public DbSet<ProjektPerson> ProjektPersons { get; set; }
        public DbSet<Erklaerbild> Erklaerbilder { get; set; }
        
        public DbSet<Material> Materialien { get; set; }
        
        // Constructor for Dependency Injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<OutstandingToken> OutstandingTokens { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
        
        public DbSet<Feedback> Feedback { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Project>().ToTable("projects");
            //modelBuilder.Entity<Projektstatus>().ToTable("projectstate");
            modelBuilder.Entity<Kooperationseinrichtung>().ToTable("Kooperationseinrichtungen");
            modelBuilder.Entity<Material>().ToTable("Materialien");

            modelBuilder.Entity<Project>()
                .Property(p => p.SdgValues)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null),
                    new ValueComparer<List<int>>(  // Für Change-Tracking
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
            
            /*modelBuilder.Entity<Project>()
                .HasMany(p => p.Sdgs)
                .WithMany(s => s.Projekte)
                .UsingEntity(j => j.ToTable("ProjektSDG"));*/
            
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Todos);
            
            // Projekt <-> Kooperationseinrichtung (through ProjektKooperationseinrichtung)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Kooperationseinrichtungen)
                .WithMany(k => k.Projekte)
                .UsingEntity(j => j.ToTable("ProjektKooperationseinrichtung"));
            
            modelBuilder.Entity<Project>()
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
                .HasOne(pp => pp.Project)
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
            
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.Category).HasConversion<int>();
                entity.Property(f => f.AffectedComponent).HasConversion<int>();

                entity.Property(f => f.Subject).HasMaxLength(200).IsRequired();
                entity.Property(f => f.Message).HasMaxLength(4000).IsRequired();
                entity.Property(f => f.AdminComment).HasMaxLength(2000);

                entity.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Recipient)
                    .WithMany()
                    .HasForeignKey("RecipientId")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            
            
        }
    }
}
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.ManyToMany;

namespace ZUMI_Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Project> Projekte { get; set; }
        public DbSet<Rolle> Rollen { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Beitrag> Beitraege { get; set; }
        public DbSet<Kooperationseinrichtung> Kooperationseinrichtungen { get; set; }
        public DbSet<Todo> Todos { get; set; }
        
        public DbSet<ProjektPerson> ProjektPersons { get; set; }
        public DbSet<Medien> Medien { get; set; }
        
        public DbSet<Material> Materialien { get; set; }
        
        // Constructor for Dependency Injection
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<OutstandingToken> OutstandingTokens { get; set; }
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
        
        public DbSet<Feedback> Feedback { get; set; }
        
        public DbSet<ProjectRole> ProjectRoles { get; set; }
        
        public DbSet<ProjektPersonRole> ProjektPersonRoles { get; set; }
        
        public DbSet<FundingItem> FundingItems => Set<FundingItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Project>().ToTable("projects");
            //modelBuilder.Entity<Projektstatus>().ToTable("projectstate");
            modelBuilder.Entity<Kooperationseinrichtung>().ToTable("Kooperationseinrichtungen");
            

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

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Todos)
                .WithOne(t => t.Project);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.FundingItems)
                .WithOne(f => f.Project);
            
            modelBuilder.Entity<Todo>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Todos)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);  // Optional: Auto-löschen von Todos bei Project-Delete
            
            modelBuilder.Entity<Material>().ToTable("Materialien")
                .HasOne(m => m.Projekt)
                .WithMany(p => p.Materialien)
                .HasForeignKey(m => m.ProjektId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Projekt <-> Kooperationseinrichtung (through ProjektKooperationseinrichtung)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Kooperationseinrichtungen)
                .WithMany(k => k.Projekte)
                .UsingEntity(j => j.ToTable("ProjektKooperationseinrichtung"));

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Materialien)
                .WithOne(m => m.Projekt)
                .OnDelete(DeleteBehavior.Cascade);
            
            // -----------------------------------------------------------
            // Konfiguration für ProjectRole
            // -----------------------------------------------------------
            modelBuilder.Entity<ProjectRole>(entity =>
            {
                entity.ToTable("ProjectRoles"); // Tabellenname

                // Ein Projekt hat viele definierte Rollen
                entity.HasOne(r => r.Project)
                    .WithMany(p => p.Roles) // <--- JETZT: Explizite Verknüpfung zur Liste im Project-Model
                    .HasForeignKey(r => r.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade); // Löscht das Projekt, werden auch die Rollen-Definitionen gelöscht
            });

            // -----------------------------------------------------------
            // Konfiguration für ProjektPerson (Die Verknüpfung)
            // -----------------------------------------------------------
            modelBuilder.Entity<ProjektPerson>(entity =>
            {
                // Composite Key 
                entity.HasKey(pp => new { pp.PersonId, pp.ProjektId });

                // Beziehung zur Person
                entity.HasOne(pp => pp.Person)
                      .WithMany(p => p.Projekte)
                      .HasForeignKey(pp => pp.PersonId);

                // Beziehung zum Projekt
                entity.HasOne(pp => pp.Project)
                      .WithMany(pr => pr.Personen)
                      .HasForeignKey(pp => pp.ProjektId);
            });
            
            // 1. Neue Tabelle registrieren
            modelBuilder.Entity<ProjektPersonRole>(entity =>
            {
                entity.ToTable("ProjektPersonRoles");
    
                // Composite Key aus 3 Teilen
                entity.HasKey(ppr => new { ppr.PersonId, ppr.ProjektId, ppr.ProjectRoleId });

                // Beziehung zu ProjektPerson
                entity.HasOne(ppr => ppr.ProjektPerson)
                    .WithMany(pp => pp.Roles)
                    .HasForeignKey(ppr => new { ppr.PersonId, ppr.ProjektId })
                    .OnDelete(DeleteBehavior.Cascade); // Wenn User fliegt, fliegen seine Rollen

                // Beziehung zu ProjectRole
                entity.HasOne(ppr => ppr.ProjectRole)
                    .WithMany()
                    .HasForeignKey(ppr => ppr.ProjectRoleId)
                    .OnDelete(DeleteBehavior.Cascade); // Wenn Rolle gelöscht wird, verlieren User diese Rolle
            });

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
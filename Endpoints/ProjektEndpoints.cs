
namespace ZUMI_Backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Data;
using Models;
using Models.DTOs;
using Models.Maps;
using Models.Enums;

public static class ProjektEndpoints
{
    public static void MapProjektEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/projekte - Projekt erstellen
        endpoints.MapPost("/projekte/create", async (CreateProjectDto dto, ApplicationDbContext db, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Results.Unauthorized();

                var userId = Guid.Parse(userIdClaim);

                // Neues Project erstellen
                var newProject = new Project
                {
                    Id = Guid.NewGuid(),
                    Projektstatus = Projektstatus.InVorbereitung  // Default-Status
                };

                // Essentials mappen
                newProject.ApplyCreateFromDto(dto);

                db.Projekte.Add(newProject);

                // Owner hinzufügen (via Through-Entity)
                db.ProjektPersons.Add(new ProjektPerson
                {
                    ProjektId = newProject.Id,
                    PersonId = userId,
                    IsOwner = true,
                    IsLiked = false,
                    IsParticipating = false  // Defaults; passe an, falls nötig
                });

                await db.SaveChangesAsync();

                // Return DTO (manuell mappen oder via Extension)
                var resultDto = newProject.MapToProjectDto();  // Dein manueller Mapper
                return Results.Created($"/api/v1/projekte/{newProject.Id}", resultDto);
            })
            .RequireAuthorization()
            .WithName("ProjektCreate")
            .WithOpenApi();
        
        endpoints.MapPut("/projekte/{id:guid}/update", async (Guid id, UpdateProjectDto dto, ApplicationDbContext db, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Results.Unauthorized();

                var userId = Guid.Parse(userIdClaim);

                // Prüfe, ob User Owner ist
                var isOwner = await db.ProjektPersons
                    .AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
                if (!isOwner) return Results.Forbid();

                var existing = await db.Projekte
                    .FirstOrDefaultAsync(p => p.Id == id);  // Besser als FindAsync für Includes
                if (existing == null) return Results.NotFound();
                
                // Speichere Snapshot vor Update (für Vergleich)
                var hasChangesBefore = db.ChangeTracker.HasChanges();
                
                // Update anwenden – EF trackt Änderungen automatisch
                existing.ApplyUpdateFromDto(dto);
                
                // Prüfe, ob Änderungen vorgenommen wurden
                var hasChangesAfter = db.ChangeTracker.HasChanges();
                var changesMade = hasChangesAfter && !hasChangesBefore;

                if (!changesMade)
                {
                    // Keine Änderungen: Bestehendes Projekt zurückgeben
                    var currentDto = existing.MapToProjectDto();
                    return Results.Ok(new { Message = "Keine Änderungen vorgenommen.", Project = currentDto });
                }

                await db.SaveChangesAsync();  // UPDATE nur für geänderte Felder + JSON für SdgValues
                
                // Änderungen gemacht: Updated Projekt zurückgeben
                var updatedDto = existing.MapToProjectDto();
                return Results.Ok(new { Message = "Projekt erfolgreich aktualisiert.", Project = updatedDto });
            })
            .RequireAuthorization()
            .WithName("ProjektUpdate")
            .WithOpenApi();

        // GET /api/v1/projekte - Alle Projekte (als DTOs)
        endpoints.MapGet("/projekte", async (ApplicationDbContext db) =>
        {
            var projekte = await db.Projekte
                .Include(p => p.Personen)
                    .ThenInclude(pp => pp.Person)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .Include(p => p.Todos)
                .ToListAsync();

            return projekte.MapToProjectDtos();
        })
        .WithName("ProjektList")
        .WithOpenApi();

        // GET /api/v1/projekte/{id} - Projekt abrufen
        endpoints.MapGet("/projekte/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Personen).ThenInclude(pp => pp.Person)                    
                .Include(p => p.Sdgs)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .Include(p => p.Todos)
                .Include(p => p.Erklaerbilder)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (projekt == null) return Results.NotFound();

            var projectDto = projekt.MapToProjectDto();
            
            return Results.Ok(projectDto);
        })
        .RequireAuthorization()
        .WithName("ProjektRetrieve")
        .WithOpenApi();
        
        // GET /api/v1/projekte/{id}/materialien
        endpoints.MapGet("/projekte/{id:guid}/materialien", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (projekt == null) return Results.NotFound();

            var materialDtos = projekt.Materialien.MapToMaterialDtos();
            
            return materialDtos.Count != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialien")
        .WithOpenApi();
        
        // GET /api/v1/projekte/{id}/materialien/gesucht
        endpoints.MapGet("/projekte/{id:guid}/materialien/gesucht", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
        
            if (projekt == null) return Results.NotFound();
            
            var materialDtos = projekt.Materialien.MapToMaterialDtos()
                .Where(m => m.Vorhanden == false)
                .ToList();
        
            return materialDtos.Count() != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialienGesucht")
        .WithOpenApi();
        
        // A person wants to like, participate or own a project
        // POST /api/v1/projekte/{id}/
        endpoints.MapPost("/projekte/{id:guid}/interaktion", async (ProjektPersonUpdateDto projektPersonUpdateDto, Guid id, ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            var projekt = await db.Projekte
                .Include(p => p.Personen)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            // Check if relationship already exists
            var existingRelation = projekt.Personen.FirstOrDefault(pp => pp.PersonId == userId);
            
            if (projekt == null) return Results.NotFound();
            
            if (existingRelation == null)
            {
                // Create new relation
                var newRelation = new ProjektPerson
                {
                    PersonId = userId,
                    ProjektId = id,
                    IsLiked = projektPersonUpdateDto.IsLiked,
                    IsOwner = projektPersonUpdateDto.IsOwner,
                    IsParticipating = projektPersonUpdateDto.IsParticipating
                };
                db.ProjektPersons.Add(newRelation);
                projekt.Personen.Add(newRelation);
            }
            else
            {
                // Update existing relation
                existingRelation.IsLiked = projektPersonUpdateDto.IsLiked;
                existingRelation.IsOwner = projektPersonUpdateDto.IsOwner;
                existingRelation.IsParticipating = projektPersonUpdateDto.IsParticipating;
            }

            await db.SaveChangesAsync();
            
            // Return the updated project
            var projectDto = projekt.MapToProjectDto();
            return Results.Ok(projectDto);
        })
        .RequireAuthorization()
        .WithName("ProjektUpdateFebe")
        .WithOpenApi();
            
        // GET /api/v1/projekte/sdg/{sdg_id:int} - Projekte gefiltert nach SDG (Enum-Wert 1-17)
        endpoints.MapGet("/projekte/sdg/{sdg_id:int}", async (int sdg_id, ApplicationDbContext db) =>
            {
                if (!Enum.IsDefined(typeof(Sdg), sdg_id))
                    return Results.BadRequest("Ungültiger SDG-Wert (muss 1-17 sein)");

                var projekte = await db.Projekte
                    .Where(p => p.SdgValues.Contains(sdg_id))  // Filter auf List<int> in JSON-Spalte
                    .Include(p => p.Projektstatus)  // Für Status-Info
                    .Include(p => p.Personen).ThenInclude(pp => pp.Person)  // Für Personen (via Through-Entity)
                    .Include(p => p.Kooperationseinrichtungen)  // Für Kooperationen
                    .Include(p => p.Materialien)  // Für Materialien
                    .Include(p => p.Todos)  // Für Todos (falls im DTO)
                    .Include(p => p.Erklaerbilder)  // Für Erklärbilder (falls im DTO)
                    .ToListAsync();

                return Results.Ok(projekte.MapToProjectDtos());  // Manueller Mapper (aus früherem Chat)
            })
            .AllowAnonymous()
            .WithName("ProjektFilteredBySDG")
            .WithOpenApi();
        
        // DELETE /api/v1/projekte/{id}/delete - Projekt löschen
        endpoints.MapDelete("/projekte/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte.FindAsync(id);
            if (projekt == null) return Results.NotFound();
            db.Projekte.Remove(projekt);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ProjektDelete")
        .WithOpenApi();

        // GET /api/v1/projekte/startseite
        // Liefert alle Projekte, bei denen der User Owner, Liker oder Mitmacher ist
        // -> nur: ProjektId, Kurztitel, Titelbild, SDG-Ids + Kategorie-Flags
        endpoints.MapGet("/projekte/startseite", async (ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Results.Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);

            var projektPersonen = await db.ProjektPersons
                .AsNoTracking()
                .Where(pp => pp.PersonId == userId &&
                             (pp.IsOwner || pp.IsLiked || pp.IsParticipating))
                .Include(pp => pp.Project)
                .ToListAsync();

            int GetCategory(ProjektPerson pp)
            {
                // Mapping:
                // 0 = Owner, 1 = Liked, 2 = Participating
                if (pp.IsOwner) return 0;
                if (pp.IsLiked) return 1;
                if (pp.IsParticipating) return 2;

                // Fallback, falls mal alle Flags false wären
                return -1;
            }

            var items = projektPersonen.Select(pp => new ProjektStartItemDto
            {
                ProjektId = pp.ProjektId,
                Kurztitel = pp.Project.Kurztitel,
                Titelbild = pp.Project.Titelbild,
                SdgIds = pp.Project.SdgValues,
                Category = GetCategory(pp)
            }).ToList();

            return Results.Ok(items);
        })
        .RequireAuthorization()
        .WithName("ProjektStartseite")
        .WithOpenApi();
        
        // GET /api/v1/projekte/discovery
        // Liefert neue Projekte, an denen der User noch keine Anteilnahme hat (Owner, Liker oder Mitmacher)
        // -> Nur: ProjektId, Kurztitel, Titelbild, SDG-Values (keine Category, da keine Beteiligung)
        // Limit auf 20 aktive Projekte für Performance (erweiterbar mit Query-Params)
        endpoints.MapGet("/projekte/discovery", async (ApplicationDbContext db, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Results.Unauthorized();
                }

                var userId = Guid.Parse(userIdClaim);

                // Hole beteiligte Projekt-IDs (für Exclusion)
                var beteiligteProjektIds = await db.ProjektPersons
                    .AsNoTracking()
                    .Where(pp => pp.PersonId == userId &&
                                 (pp.IsOwner || pp.IsLiked || pp.IsParticipating))
                    .Select(pp => pp.ProjektId)
                    .ToListAsync();

                var projekte = await db.Projekte
                    .AsNoTracking()
                    .Where(p => !beteiligteProjektIds.Contains(p.Id) &&  // Exclusion: Keine eigenen Projekte
                                p.Projektstatus != Projektstatus.Archiviert)  // Nur aktive (optional: passe Filter an)
                    .OrderBy(p => p.LetztesUpdate)  // Neueste zuerst (optional)
                    .Take(20)  // Limit für Discovery (erweiterbar mit ?limit=50)
                    .Select(p => new ProjektStartItemDto  // Projiziere direkt zu DTO (effizient)
                    {
                        ProjektId = p.Id,
                        Kurztitel = p.Kurztitel,
                        Titelbild = p.Titelbild,
                        SdgIds = p.SdgValues,  // List<int> als SDG-Values
                        Category = -1  // Keine Beteiligung (Fallback)
                    })
                    .ToListAsync();

                return Results.Ok(projekte);
            })
            .RequireAuthorization()
            .WithName("ProjektDiscovery")
            .WithOpenApi();
    }
}
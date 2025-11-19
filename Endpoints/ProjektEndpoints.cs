using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using ZUMI_Backend.Models.Maps;

namespace ZUMI_Backend.Endpoints;

public static class ProjektEndpoints
{
    public static void MapProjektEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/projekte - Projekt erstellen
        endpoints.MapPost("/projekte/create/", async (Project newProject, ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            newProject.Id = Guid.NewGuid();
            
            // Projekt is always aktive at creation.
            // Hardinsertet into the DB -> cannot be null!
            if(newProject.ProjektstatusId == null)
                newProject.ProjektstatusId = db.Projektstatuses.FirstOrDefault(ps => ps.Bezeichnung == "Aktiv")!.Id;
                
            db.Projekte.Add(newProject);
            
            db.ProjektPersons.Add(new ProjektPerson
            {
                ProjektId = newProject.Id,
                PersonId = userId,
                IsOwner = true
            });
            
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/projekte/{newProject.Id}", newProject);
        })
        .RequireAuthorization()
        .WithName("ProjektCreate")
        .WithOpenApi();
        
        // PUT /api/v1/projekte/{id}/update - Projekt aktualisieren
        endpoints.MapPut("/projekte/{id:guid}/update", async (Guid id, Project updated, ApplicationDbContext db) =>
        {
            var existing = await db.Projekte.FindAsync(id);
            if (existing == null) return Results.NotFound();
            existing.Kurztitel = updated.Kurztitel;
            existing.Kurzbeschreibung = updated.Kurzbeschreibung;
            existing.Titelbild = updated.Titelbild;
            existing.Beschreibung = updated.Beschreibung;
            existing.Vorbereitungszeitraum = updated.Vorbereitungszeitraum;
            existing.Umsetzungszeitraum = updated.Umsetzungszeitraum;
            existing.StandortLink = updated.StandortLink;
            existing.Adresse = updated.Adresse;
            existing.Plz = updated.Plz;
            existing.Spendeninformationen = updated.Spendeninformationen;
            existing.WeitereInfos = updated.WeitereInfos;
            existing.LetztesUpdate = updated.LetztesUpdate;
            existing.ProjektstatusId = updated.ProjektstatusId;

            existing.Sdgs.Clear();
            foreach (var sdg in updated.Sdgs)
            {
                var attachedSdg = await db.SustainableDevelopmentGoals.FindAsync(sdg.Id);
                if (attachedSdg != null)
                {
                    existing.Sdgs.Add(attachedSdg);
                }
            }
            // Ähnlich für andere Many-to-Many

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ProjektUpdate")
        .WithOpenApi();

        // GET /api/v1/projekte - Alle Projekte (als DTOs)
        endpoints.MapGet("/projekte", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var projekte = await db.Projekte
                .Include(p => p.Projektstatus)
                .Include(p => p.Sdgs)
                .Include(p => p.Personen)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .Include(p => p.Todos)
                .ToListAsync();

            return mapper.Map<List<ProjectDto>>(projekte);
        })
        .RequireAuthorization()
        .WithName("ProjektList")
        .WithOpenApi();

        // GET /api/v1/projekte/{id} - Projekt abrufen
        endpoints.MapGet("/projekte/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Projektstatus)
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
        endpoints.MapGet("/projekte/{id:guid}/materialien", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (projekt == null) return Results.NotFound();

            var materialDtos = mapper.Map<List<MaterialDto>>(projekt.Materialien);
            
            return materialDtos.Count != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialien")
        .WithOpenApi();
        
        // GET /api/v1/projekte/{id}/materialien/gesucht
        endpoints.MapGet("/projekte/{id:guid}/materialien/gesucht", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
        
            if (projekt == null) return Results.NotFound();

            var materialDtos = mapper.Map<List<MaterialDto>>(projekt.Materialien).Where(m => m.Vorhanden == true);
        
            return materialDtos.Count() != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialienGesucht")
        .WithOpenApi();
        
        // A person wants to like, participate or own a project
        // POST /api/v1/projekte/{id}/
        endpoints.MapPost("/projekte/{id:guid}/interaktion", async (ProjektPersonUpdateDto projektPersonUpdateDto, Guid id, ApplicationDbContext db, IMapper mapper, HttpContext http) =>
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
            return Results.Ok(mapper.Map<ProjectDto>(projekt));
        })
        .RequireAuthorization()
        .WithName("ProjektUpdateFebe")
        .WithOpenApi();
            
        // GET /api/v1/projekte/sdg/{sdg_id} - Filtered by SDG
        endpoints.MapGet("/projekte/sdg/{sdg_id:guid}", async (Guid sdg_id, ApplicationDbContext db) =>
        {
            return await db.Projekte
                .Where(p => p.Sdgs.Any(s => s.Id == sdg_id))
                .Include(p => p.Projektstatus)
                .Include(p => p.Sdgs)
                .Include(p => p.Personen)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .ToListAsync();
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
                    .ThenInclude(p => p.Sdgs)
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
                SdgIds = pp.Project.Sdgs.Select(s => s.Id).ToList(),
                Category = GetCategory(pp)
            }).ToList();

            return Results.Ok(items);
        })
        .RequireAuthorization()
        .WithName("ProjektStartseite")
        .WithOpenApi();
    }
}
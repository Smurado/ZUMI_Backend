using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;

namespace ZUMI_Backend.Endpoints;

public static class ProjektEndpoints
{
    public static void MapProjektEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/projekte - Projekt erstellen
        endpoints.MapPost("/projekte/create/", async (Projekt newProjekt, ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            newProjekt.Id = Guid.NewGuid();
            
            // Projekt is always aktive at creation.
            // Hardinsertet into the DB -> cannot be null!
            if(newProjekt.ProjektstatusId == null)
                newProjekt.ProjektstatusId = db.Projektstatuses.FirstOrDefault(ps => ps.Bezeichnung == "Aktiv")!.Id;
                
            db.Projekte.Add(newProjekt);
            
            
            db.ProjektPersons.Add(new ProjektPerson
            {
                ProjektId = newProjekt.Id,
                PersonId = userId,
                IsOwner = true
            });
            
            
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/projekte/{newProjekt.Id}", newProjekt);
        })
        .RequireAuthorization()
        .WithName("ProjektCreate")
        .WithOpenApi();
        
        // PUT /api/v1/projekte/{id}/update - Projekt aktualisieren
        endpoints.MapPut("/projekte/{id:guid}/update", async (Guid id, Projekt updated, ApplicationDbContext db) =>
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
                .ToListAsync();

            return mapper.Map<List<ProjektDto>>(projekte);
        })
        .RequireAuthorization()
        .WithName("ProjektList")
        .WithOpenApi();

        // GET /api/v1/projekte/meine 
        endpoints.MapGet("/projekte/meine", async (ApplicationDbContext db, HttpContext http, IMapper mapper) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            var projekte = await db.Projekte
                .Where(p => p.Personen.Any(pe => pe.PersonId == userId))
                .Include(p => p.Projektstatus)
                .Include(p => p.Sdgs)
                .Include(p => p.Personen)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .ToListAsync();

            return mapper.Map<List<ProjektDto>>(projekte);
        })
        .RequireAuthorization()
        .WithName("ProjektMeine")
        .WithOpenApi();

        // GET /api/v1/projekte/{id} - Projekt abrufen
        endpoints.MapGet("/projekte/{id:guid}", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Projektstatus)
                .Include(p => p.Sdgs)
                .Include(p => p.Personen)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
            return projekt != null ? Results.Ok(mapper.Map<ProjektDto>(projekt)) : Results.NotFound();
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

            var materialDtos = mapper.Map<List<MaterialDto>>(projekt.Materialien).Where(m => m.vorhanden == true);
        
            return materialDtos.Count() != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialienGesucht")
        .WithOpenApi();
        
        // A person wants to like, participate or own a project
        // POST /api/v1/projekte/{id}/febe/
        endpoints.MapPost("/projekte/{id:guid}/febe", async (ProjektPersonUpdateDto projektPersonUpdateDto, Guid id, ApplicationDbContext db, IMapper mapper, HttpContext http) =>
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
            return Results.Ok(mapper.Map<ProjektDto>(projekt));
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
    }
}
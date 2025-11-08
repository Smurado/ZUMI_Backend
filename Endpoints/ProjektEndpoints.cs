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
        endpoints.MapPost("/projekte", async (Projekt newProjekt, ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            newProjekt.Id = Guid.NewGuid();
            db.Projekte.Add(newProjekt);
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

        // GET /api/v1/projekte/meine - Eigene Projekte (als DTOs)
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
    }
}
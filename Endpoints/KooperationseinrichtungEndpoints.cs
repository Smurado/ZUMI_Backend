using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class KooperationseinrichtungEndpoints
{
    public static void MapKooperationseinrichtungEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/kooperationseinrichtung - Create
        endpoints.MapPost("/kooperationseinrichtung", async (Kooperationseinrichtung newKooperationseinrichtung, ApplicationDbContext db) =>
        {
            db.Kooperationseinrichtungen.Add(newKooperationseinrichtung);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/kooperationseinrichtung/{newKooperationseinrichtung.Id}", newKooperationseinrichtung);
        })
        .RequireAuthorization()
        .WithName("KooperationseinrichtungCreate")
        .WithOpenApi();

        // GET /api/v1/kooperationseinrichtung - List
        endpoints.MapGet("/kooperationseinrichtung", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var kooperationseinrichtung = await db.Kooperationseinrichtungen.ToListAsync();
            return mapper.Map<List<KooperationseinrichtungDto>>(kooperationseinrichtung);
        })
        .AllowAnonymous()
        .WithName("KooperationseinrichtungList")
        .WithOpenApi();

        // GET /api/v1/kooperationseinrichtung/{id} - Retrieve
        endpoints.MapGet("/kooperationseinrichtung/{id:guid}", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var kooperationseinrichtung = await db.Kooperationseinrichtungen.FindAsync(id);
            return kooperationseinrichtung != null ? Results.Ok(mapper.Map<KooperationseinrichtungDto>(kooperationseinrichtung)) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("KooperationseinrichtungRetrieve")
        .WithOpenApi();

        // PUT /api/v1/kooperationseinrichtung/{id}/update - Update
        endpoints.MapPut("/kooperationseinrichtung/{id:guid}/update", async (Guid id, Kooperationseinrichtung updated, ApplicationDbContext db) =>
        {
            var existing = await db.Kooperationseinrichtungen.FindAsync(id);
            if (existing == null) return Results.NotFound();
            // Update Properties hier ergänzen
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("KooperationseinrichtungUpdate")
        .WithOpenApi();

        // DELETE /api/v1/kooperationseinrichtung/{id}/delete - Delete
        endpoints.MapDelete("/kooperationseinrichtung/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var kooperationseinrichtung = await db.Kooperationseinrichtungen.FindAsync(id);
            if (kooperationseinrichtung == null) return Results.NotFound();
            db.Kooperationseinrichtungen.Remove(kooperationseinrichtung);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("KooperationseinrichtungDelete")
        .WithOpenApi();
    }
} 
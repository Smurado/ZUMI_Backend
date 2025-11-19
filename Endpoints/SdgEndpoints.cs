using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class SdgEndpoints
{
    public static void MapSdgEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/sdg - Create
        endpoints.MapPost("/sdg", async (SustainableDevelopmentGoal newSdg, ApplicationDbContext db) =>
        {
            db.SustainableDevelopmentGoals.Add(newSdg);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/sdg/{newSdg.Id}", newSdg);
        })
        .RequireAuthorization()
        .WithName("SdgCreate")
        .WithOpenApi();

        // GET /api/v1/sdg - List
        endpoints.MapGet("/sdg", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var sdgs = await db.SustainableDevelopmentGoals.ToListAsync();
            return mapper.Map<List<SdgDto>>(sdgs);
        })
        .AllowAnonymous()
        .WithName("SdgList")
        .WithOpenApi();

        // GET /api/v1/sdg/{id} - Retrieve
        endpoints.MapGet("/sdg/{id:guid}", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var sdg = await db.SustainableDevelopmentGoals.FindAsync(id);
            return sdg != null ? Results.Ok(mapper.Map<SdgDto>(sdg)) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("SdgRetrieve")
        .WithOpenApi();

        // PUT /api/v1/sdg/{id}/update - Update
        endpoints.MapPut("/sdg/{id:guid}/update", async (Guid id, SustainableDevelopmentGoal updated, ApplicationDbContext db) =>
        {
            var existing = await db.SustainableDevelopmentGoals.FindAsync(id);
            if (existing == null) return Results.NotFound();
            // Update Properties hier ergänzen
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("SdgUpdate")
        .WithOpenApi();

        // DELETE /api/v1/sdg/{id}/delete - Delete
        endpoints.MapDelete("/sdg/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var sdg = await db.SustainableDevelopmentGoals.FindAsync(id);
            if (sdg == null) return Results.NotFound();
            db.SustainableDevelopmentGoals.Remove(sdg);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("SdgDelete")
        .WithOpenApi();
    }
} 
/*using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class RolleEndpoints
{
    public static void MapRolleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/rolle - Create
        endpoints.MapPost("/rolle", async (Rolle newRolle, ApplicationDbContext db) =>
        {
            db.Rollen.Add(newRolle);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/rolle/{newRolle.Id}", newRolle);
        })
        .RequireAuthorization()
        .WithName("RolleCreate")
        .WithOpenApi();

        // GET /api/v1/rolle - List
        endpoints.MapGet("/rolle", async (ApplicationDbContext db) =>
        {
            var rollen =  await db.Rollen.ToListAsync();
            return mapper.Map<List<RolleDto>>(rollen);
        })
        .AllowAnonymous()
        .WithName("RolleList")
        .WithOpenApi();

        // GET /api/v1/rolle/{id} - Retrieve
        endpoints.MapGet("/rolle/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var rolle = await db.Rollen.FindAsync(id);
            return rolle != null ? Results.Ok(mapper.Map<RolleDto>(rolle)) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("RolleRetrieve")
        .WithOpenApi();

        // PUT /api/v1/rolle/{id}/update - Update
        endpoints.MapPut("/rolle/{id:guid}/update", async (Guid id, Rolle updated, ApplicationDbContext db) =>
        {
            var existing = await db.Rollen.FindAsync(id);
            if (existing == null) return Results.NotFound();
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("RolleUpdate")
        .WithOpenApi();

        // DELETE /api/v1/rolle/{id}/delete - Delete
        endpoints.MapDelete("/rolle/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var rolle = await db.Rollen.FindAsync(id);
            if (rolle == null) return Results.NotFound();
            db.Rollen.Remove(rolle);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("RolleDelete")
        .WithOpenApi();
    }
}*/
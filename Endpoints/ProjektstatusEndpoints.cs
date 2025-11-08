using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class ProjektstatusEndpoints
{
    public static void MapProjektstatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/projektstatus - Create
        endpoints.MapPost("/projektstatus", async (Projektstatus newStatus, ApplicationDbContext db) =>
        {
            db.Projektstatuses.Add(newStatus);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/projektstatus/{newStatus.Id}", newStatus);
        })
        .RequireAuthorization()
        .WithName("ProjektstatusCreate")
        .WithOpenApi();

        // GET /api/v1/projektstatus - List
        endpoints.MapGet("/projektstatus", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var projektstatuses = await db.Projektstatuses.ToListAsync();
            return mapper.Map<List<Projektstatus>>(projektstatuses);
        })
        .AllowAnonymous()
        .WithName("ProjektstatusList")
        .WithOpenApi();

        // GET /api/v1/projektstatus/{id} - Retrieve
        endpoints.MapGet("/projektstatus/{id:guid}", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var status = await db.Projektstatuses.FindAsync(id);
            return status != null ? Results.Ok(mapper.Map<ProjektstatusDto>(status)) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("ProjektstatusRetrieve")
        .WithOpenApi();

        // PUT /api/v1/projektstatus/{id}/update - Update
        endpoints.MapPut("/projektstatus/{id:guid}/update", async (Guid id, Projektstatus updated, ApplicationDbContext db) =>
        {
            var existing = await db.Projektstatuses.FindAsync(id);
            if (existing == null) return Results.NotFound();
            // Update Properties hier ergänzen
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ProjektstatusUpdate")
        .WithOpenApi();

        // DELETE /api/v1/projektstatus/{id}/delete - Delete
        endpoints.MapDelete("/projektstatus/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var status = await db.Projektstatuses.FindAsync(id);
            if (status == null) return Results.NotFound();
            db.Projektstatuses.Remove(status);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ProjektstatusDelete")
        .WithOpenApi();
    }
} 
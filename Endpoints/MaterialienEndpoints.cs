using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class MaterialienEndpoints
{
    public static void MapMaterialienEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST  - Create /api/v1/materialien/create/
        endpoints.MapPost("/materialien/create/", async (Material newMaterial, ApplicationDbContext db) =>
        {
            db.Materialien.Add(newMaterial);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/material/{newMaterial.Id}", newMaterial);
        })
        .RequireAuthorization()
        .WithName("MaterialCreate")
        .WithOpenApi();

        // GET /api/v1/material - List
        endpoints.MapGet("/material", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var materialien = await db.Materialien.ToListAsync();
            return mapper.Map<List<MaterialDto>>(materialien);
        })
        .AllowAnonymous()
        .WithName("MaterialList")
        .WithOpenApi();

        // GET /api/v1/material/{id} - Retrieve
        endpoints.MapGet("/material/{id:guid}", async (Guid id, ApplicationDbContext db, IMapper mapper) =>
        {
            var material = await db.Materialien.FindAsync(id);
            return material != null ? Results.Ok(mapper.Map<MaterialDto>(material)) : Results.NotFound();
        })
        .AllowAnonymous()
        .WithName("MaterialRetrieve")
        .WithOpenApi();

        // PUT /api/v1/material/{id}/update - Update
        endpoints.MapPut("/material/{id:guid}/update", async (Guid id, Material updated, ApplicationDbContext db) =>
        {
            var existing = await db.Materialien.FindAsync(id);
            if (existing == null) return Results.NotFound();
            // Update Properties hier ergänzen
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("MaterialUpdate")
        .WithOpenApi();

        // DELETE /api/v1/material/{id}/delete - Delete
        endpoints.MapDelete("/material/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var material = await db.Materialien.FindAsync(id);
            if (material == null) return Results.NotFound();
            db.Materialien.Remove(material);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("MaterialDelete")
        .WithOpenApi();
    }
} 
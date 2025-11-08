using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class ErklaerbildEndpoints
{
    public static void MapErklaerbildEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // DELETE /api/v1/erklaerbilder/{id}/delete
        endpoints.MapDelete("/erklaerbilder/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
            {
                var bild = await db.Erklaerbilder.FindAsync(id);
                if (bild == null) return Results.NotFound();
                db.Erklaerbilder.Remove(bild);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("ErklaerbildDelete")
            .WithOpenApi();

        // GET /api/v1/projekte/{projekt_id}/erklaerbilder - List for Project
        endpoints.MapGet("/projekte/{projekt_id:guid}/erklaerbilder", async (Guid projektid, ApplicationDbContext db, IMapper mapper) =>
            {
                var erklaerbild = await db.Erklaerbilder.Where(e => e.ProjektId == projektid).ToListAsync();
                return mapper.Map<List<ErklaerbildDto>>(erklaerbild);
            })
            .RequireAuthorization()
            .WithName("ErklaerbildList")
            .WithOpenApi();
    }
}
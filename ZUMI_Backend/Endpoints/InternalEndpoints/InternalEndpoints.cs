namespace ZUMI_Backend.Endpoints.InternalEndpoints;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models.Enums;

public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Gruppe für interne Aufrufe (vom Converter)
        // Die Route wird sein: /api/v1/internal/callback/{mediaId}
        var group = endpoints.MapGroup("/internal").WithTags("Internal");

        group.MapPost("/callback/{mediaId:guid}", async (Guid mediaId, [FromBody] CallbackDto request, ApplicationDbContext db) =>
            {
                Console.WriteLine($"[Internal] Callback erhalten für {mediaId}. Neuer Status: {request.Status}");

                var medium = await db.Medien.FindAsync(mediaId);
                if (medium == null)
                {
                    return Results.NotFound();
                }

                // Status updaten
                // Wir casten den int (1-4) einfach auf das Enum
                medium.Status = (MediaStatus)request.Status;

                await db.SaveChangesAsync();

                return Results.Ok();
            })
            .AllowAnonymous(); // WICHTIG: Der Converter hat keinen User-Login, er darf das so aufrufen.
    }
}

// Kleines DTO für die Daten, die der Converter schickt
public record CallbackDto(int Status);
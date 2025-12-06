// Endpoints/FeedbackEndpoints.cs
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using ZUMI_Backend.Models.Enums;
using ZUMI_Backend.Extensions;
using ZUMI_Backend.Models.Maps;

namespace ZUMI_Backend.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/feedback")
                            .RequireAuthorization();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 1. Enum-Listen für Frontend (anonym erlaubt – werden beim Login gebraucht)
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapGet("/categories", () =>
        {
            var values = Enum.GetValues<FeedbackCategory>()
                .Select(c => new
                {
                    Value = c.ToString(),
                    Label = c.GetDisplayName(),
                    NumericValue = (int)c
                })
                .OrderBy(x => x.NumericValue)
                .ToList();

            return Results.Ok(values);
        })
        .AllowAnonymous()
        .WithName("FeedbackCategories")
        .WithOpenApi();

        group.MapGet("/affected-components", () =>
        {
            var values = Enum.GetValues<FeedbackAffectedComponent>()
                .Select(c => new
                {
                    Value = c.ToString(),
                    Label = c.GetDisplayName(),
                    NumericValue = (int)c
                })
                .OrderBy(x => x.NumericValue)
                .ToList();

            return Results.Ok(values);
        })
        .AllowAnonymous()
        .WithName("FeedbackAffectedComponents")
        .WithOpenApi();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 2. Feedback erstellen
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapPost("/", async (CreateFeedbackDto dto, ApplicationDbContext db, HttpContext http) =>
        {
            Person? sender = null; // Standard: anonym (null für Guests)

            // Optional: Wenn authentifiziert, lade den User als Sender
            if (http.User.Identity?.IsAuthenticated == true)
            {
                var userIdString = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                 // Falls Claim fehlt, ablehnen
                if (string.IsNullOrEmpty(userIdString)) return Results.Unauthorized();

                var userId = Guid.Parse(userIdString);
                sender = await db.Persons.FindAsync(userId);
                // User nicht gefunden
                if (sender == null) return Results.Unauthorized();
            }

            Person? recipient = null; // Standard: null, falls nicht angegeben
            if (dto.RecipientId.HasValue)
            {
                recipient = await db.Persons.FindAsync(dto.RecipientId.Value);
                if (recipient == null) return Results.BadRequest("Recipient nicht gefunden");
            }

            var feedback = dto.MapToEntity(sender, recipient);
            feedback.User = sender; // Kann null sein für anonymes Feedback
            feedback.Recipient = recipient;

            db.Feedback.Add(feedback);
            await db.SaveChangesAsync();


            var resultDto = feedback.MapToFeedbackDto();
            return Results.Created($"/api/v1/feedback/{feedback.Id}", resultDto);
        })
        .AllowAnonymous() // Endpunkt öffentlich – keine Authentifizierung erforderlich
        .WithName("FeedbackCreate")
        .WithOpenApi();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 3. Meine gesendeten Feedbacks
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapGet("/my", async (ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var myFeedback = await db.Feedback
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => f.User != null && f.User.Id == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var dtos = myFeedback.MapToFeedbackDtos();
            
            return Results.Ok(myFeedback);
        })
        .RequireAuthorization()    
        .WithName("FeedbackMy")
        .WithOpenApi();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 4. Alle Feedbacks (nur für Admins/Support)
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var allFeedback = await db.Feedback
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Recipient)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
            
            var dtos = allFeedback.MapToFeedbackDetailDtos();

            return Results.Ok(allFeedback);
        })
        .RequireAuthorization("AdminPolicy")
        .WithName("FeedbackListAdmin")
        .WithOpenApi();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 5. Feedback als erledigt markieren
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapPatch("/{id:guid}/resolve", async (Guid id, ApplicationDbContext db) =>
        {
            var feedback = await db.Feedback.FindAsync(id);
            if (feedback == null) return Results.NotFound();

            feedback.IsRead = true;
            feedback.IsResolved = true;
            feedback.ResolvedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization("AdminPolicy")
        .WithName("FeedbackResolve")
        .WithOpenApi();
        
    }
}
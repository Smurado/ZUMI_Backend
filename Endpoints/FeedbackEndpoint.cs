// Endpoints/FeedbackEndpoints.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using ZUMI_Backend.Models.Enums;
using ZUMI_Backend.Extensions;
using AutoMapper.QueryableExtensions;

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
        group.MapPost("/", async (CreateFeedbackDto dto, ApplicationDbContext db, HttpContext http, IMapper mapper) =>
        {
            Person? sender = null; // Standard: anonym (null für Guests)

            // Optional: Wenn authentifiziert, lade den User als Sender
            if (http.User.Identity?.IsAuthenticated == true)
            {
                var userIdString = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Results.Unauthorized(); // Falls Claim fehlt, ablehnen
                }

                var userId = Guid.Parse(userIdString);
                sender = await db.Persons.FindAsync(userId);
                if (sender == null)
                {
                    return Results.Unauthorized(); // User nicht gefunden
                }
            }

            Person? recipient = null; // Standard: null, falls nicht angegeben
            if (dto.RecipientId.HasValue)
            {
                recipient = await db.Persons.FindAsync(dto.RecipientId.Value);
                if (recipient == null)
                {
                    return Results.BadRequest("Recipient nicht gefunden");
                }
            }

            var feedback = mapper.Map<Feedback>(dto);
            feedback.User = sender; // Kann null sein für anonymes Feedback
            feedback.Recipient = recipient;

            db.Feedback.Add(feedback);
            await db.SaveChangesAsync();

            var resultDto = mapper.Map<FeedbackDto>(feedback);
            return Results.Created($"/api/v1/feedback/{feedback.Id}", resultDto);
        })
        .AllowAnonymous() // Endpunkt öffentlich – keine Authentifizierung erforderlich
        .WithName("FeedbackCreate")
        .WithOpenApi();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 3. Meine gesendeten Feedbacks
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapGet("/my", async (ApplicationDbContext db, HttpContext http, IMapper mapper) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var myFeedback = await db.Feedback
                .AsNoTracking()
                .Where(f => f.User.Id == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ProjectTo<FeedbackDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return Results.Ok(myFeedback);
        })
        .WithName("FeedbackMy")
        .WithOpenApi();

        // ╔══════════════════════════════════════════════════════════════════════
        // ║ 4. Alle Feedbacks (nur für Admins/Support)
        // ╚══════════════════════════════════════════════════════════════════════
        group.MapGet("/", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var allFeedback = await db.Feedback
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Recipient)
                .OrderByDescending(f => f.CreatedAt)
                .ProjectTo<FeedbackDetailDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return Results.Ok(allFeedback);
        })
        .RequireAuthorization("AdminPolicy") // oder "SupportPolicy" je nach dem was du hast
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
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

namespace ZUMI_Backend.Endpoints;

public static class PersonEndpoints
{
    public static void MapPersonEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // PUT /api/v1/personen/{id}/update - Person aktualisieren (Own only)
        endpoints.MapPut("/personen/{id:guid}/update", async (Guid id, Person updated, ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            if (id != userId) return Results.Forbid();
            var existing = await db.Persons.FindAsync(id);
            if (existing == null) return Results.NotFound();
            // Update Properties hier ergänzen, z.B. existing.Name = updated.Name;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("UpdatePerson")
        .WithOpenApi();

        // GET /api/v1/personen - Alle Personen listen
        endpoints.MapGet("/personen", async (ApplicationDbContext db, IMapper mapper) =>
        {
            var persons = await db.Persons.ToListAsync();
            return mapper.Map<List<PersonDto>>(persons);
        })
        .RequireAuthorization()
        .WithName("PersonList")
        .WithOpenApi();

        // POST /api/v1/personen - Person erstellen
        endpoints.MapPost("/personen", async (Person newPerson, ApplicationDbContext db) =>
        {
            if (string.IsNullOrEmpty(newPerson.Password))
            {
                return Results.BadRequest("Passwort ist erforderlich.");
            }
            if (string.IsNullOrEmpty(newPerson.Email)) return Results.BadRequest("Email ist erforderlich.");

            newPerson.Password = BCrypt.Net.BCrypt.HashPassword(newPerson.Password);
            db.Persons.Add(newPerson);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/personen/{newPerson.Id}", newPerson);
        })
        .AllowAnonymous()
        .WithName("CreatePerson")
        .WithOpenApi();
    }
}
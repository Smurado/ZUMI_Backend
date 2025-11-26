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
        // PUT /api/v1/personen/update - Person aktualisieren (Own only, keine ID im Path)
        endpoints.MapPut("/personen/update", async (UpdatePersonDto updated, ApplicationDbContext db, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Results.Unauthorized();

                var userId = Guid.Parse(userIdClaim);

                var existing = await db.Persons.FindAsync(userId);
                if (existing == null) return Results.NotFound();

                // Properties zuweisen – EF Core trackt Änderungen automatisch
                existing.Email = updated.Email ?? existing.Email;
                existing.Password = updated.Password != null ? BCrypt.Net.BCrypt.HashPassword(updated.Password) : existing.Password;  // Hash nur bei Änderung
                existing.Plz = updated.Plz ?? existing.Plz;
                existing.Land = updated.Land ?? existing.Land;
                existing.Ort = updated.Ort ?? existing.Ort;
                existing.Sprache = updated.Sprache ?? existing.Sprache;
                existing.Interessen = updated.Interessen ?? existing.Interessen;
                existing.Staerken = updated.Staerken ?? existing.Staerken;
                existing.Avatar = updated.Avatar ?? existing.Avatar;
                existing.FirstName = updated.FirstName ?? existing.FirstName;
                existing.LastName = updated.LastName ?? existing.LastName;
                existing.Altersgruppe = updated.Altersgruppe;

                await db.SaveChangesAsync();  // EF generiert UPDATE nur für geänderte Felder
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("UpdatePerson")
            .WithOpenApi();
        
        // GET /api/v1/whoami - Alle Personen listen
        endpoints.MapGet("/whoami", async (ApplicationDbContext db, IMapper mapper, HttpContext http) => 
            {
            
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            
            if (userId == Guid.Empty) return Results.Unauthorized();
            
            var person = await db.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == userId);
            
            if (person == null) return Results.NotFound("Person not found");
            
            var personDto = mapper.Map<PersonDto>(person);
            return Results.Ok(personDto);
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
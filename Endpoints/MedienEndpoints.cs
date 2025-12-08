namespace ZUMI_Backend.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Data;
using Models;
using Models.Enums;
using Models.Maps;

public static class MedienEndpoints
{
    private static readonly string UploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
    private static readonly string VideoOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "output", "videos");

    public static void MapBildEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/projekte/{id:guid}/erklaerbilder - Erklärbild hochladen (Owner only)
        endpoints.MapPost("/projekte/{id:guid}/erklaerbilder", async (Guid id, IFormFile? file, ApplicationDbContext db, HttpContext http) =>
        {
            // Logging für Debugging (schau in Console/Logs)
            Console.WriteLine($"Upload-Request: ID={id}, File-Name={file?.FileName}, Content-Type={file?.ContentType}, Length={file?.Length ?? 0}");

            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            // Owner-Check
            var isOwner = await db.ProjektPersons.AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();

            // Validierung
            if (file == null || file.Length == 0) 
                return Results.BadRequest("Keine Datei hochgeladen.");

            if (file.Length > 5 * 1024 * 1024) 
                return Results.BadRequest("Datei zu groß (max 5MB).");

            if (!file.ContentType.StartsWith("image/")) 
                return Results.BadRequest("Nur Bilder erlaubt (jpg, png, gif).");

            var existingProject = await db.Projekte.FindAsync(id);
            if (existingProject == null) return Results.NotFound("Projekt nicht gefunden.");

            try
            {
                // Ordner erstellen
                var projektDir = Path.Combine(UploadPath, "projekte", id.ToString());
                Directory.CreateDirectory(projektDir);

                // Unique Dateiname
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(projektDir, fileName);
                var relativeUrl = $"/uploads/projekte/{id}/{fileName}";

                // Speichern
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // In DB speichern
                var neuesBild = new Medien
                {
                    ProjektId = id,
                    Url = relativeUrl
                };
                db.Medien.Add(neuesBild);
                await db.SaveChangesAsync();

                var resultDto = neuesBild.MapToMedienDto();
                return Results.Created($"/api/v1/projekte/{id}/erklaerbilder/{neuesBild.Id}", resultDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload-Fehler: {ex.Message}");  // Log für Debug
                return Results.Problem($"Fehler beim Hochladen: {ex.Message}");
            }
        })
        .DisableAntiforgery()  // Fix: Deaktiviert CSRF-Check für diesen Endpoint (sicher mit JWT)
        .RequireAuthorization()
        .WithName("ErklaerbildCreate")
        .WithOpenApi()
        .Accepts<MultipartFormDataContent>("multipart/form-data");  // Für File-Upload

        // GET /api/v1/projekte/{id:guid}/erklaerbilder - Liste aller Erklärbilder (Public)
        endpoints.MapGet("/projekte/{id:guid}/erklaerbilder", async (Guid id, ApplicationDbContext db) =>
        {
            var bilder = await db.Medien
                .Where(e => e.ProjektId == id)
                .ToListAsync();

            var dtos = bilder.MapToMedienDtos();
            return Results.Ok(dtos);
        })
        .AllowAnonymous()
        .WithName("ErklaerbildList")
        .WithOpenApi();
        
        // GET /api/v1/images/{bildId:guid} - Bild streamen (Auth required, Owner-Check)
        endpoints.MapGet("/images/{bildId:guid}", async (Guid bildId, ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            // Lade Bild und prüfe Ownership (via ProjektPerson)
            var bild = await db.Medien
                .Include(e => e.Project)  // Für Owner-Check
                .ThenInclude(p => p.Personen)  // Personen via Through
                .FirstOrDefaultAsync(e => e.Id == bildId);

            if (bild == null) return Results.NotFound();

            // Owner-Check: User muss Owner des Projekts sein
            var isOwner = bild.Project.Personen.Any(pp => pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();

            // Datei laden und streamen (kein Download-Header, um Inline-Viewing zu erlauben)
            var filePath = Path.Combine(UploadPath, bild.Url.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ? bild.Url.Substring("/uploads".Length).TrimStart('/') : bild.Url.TrimStart('/')); 
            if (!File.Exists(filePath)) return Results.NotFound();

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(bild.Url);  // Helper: z. B. ".jpg" → "image/jpeg"

            return Results.File(fileBytes, contentType, enableRangeProcessing: true);  // Range-Support für Partial-Requests
        })
        .WithName("ImageServe")
        .WithOpenApi();

        // DELETE /api/v1/projekte/{id:guid}/erklaerbilder/{bildId:guid} - Löschen (Owner only)
        endpoints.MapDelete("/projekte/{id:guid}/erklaerbilder/{bildId:guid}", async (Guid id, Guid bildId, ApplicationDbContext db, HttpContext http) =>
        {
            // Owner-Check (wie oben)
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var isOwner = await db.ProjektPersons.AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();

            var bild = await db.Medien.FindAsync(bildId);
            if (bild == null || bild.ProjektId != id) return Results.NotFound();

            // Datei löschen
            var filePath = Path.Combine(UploadPath, bild.Url.TrimStart('/'));
            if (File.Exists(filePath)) File.Delete(filePath);

            db.Medien.Remove(bild);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ErklaerbildDelete")
        .WithOpenApi();
    }
    
    // Helper: Datei validieren und speichern
    private static async Task<string> SaveFileAsync(IFormFile file, string basePath)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Keine gültige Datei angegeben.");

        // Validierung
        if (file.Length > 5 * 1024 * 1024)  // Max 5 MB
            throw new ArgumentException("Datei zu groß (max. 5 MB).");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType))
            throw new ArgumentException("Nur Bilder (JPEG, PNG, GIF) erlaubt.");

        // Ordner erstellen, falls nicht vorhanden
        Directory.CreateDirectory(basePath);

        // Unique Dateiname generieren (Guid + Original-Endung)
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(basePath, fileName);

        // Datei speichern
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Relative URL zurückgeben (z. B. "/uploads/projekte/{id}/{fileName}")
        var relativeUrl = $"/uploads{basePath.Replace(Directory.GetCurrentDirectory(), "").Replace("\\", "/").Replace("//", "/")}";
        relativeUrl = Path.Combine(relativeUrl, fileName).Replace("\\", "/");

        return relativeUrl;
    }
    
    // Helper: Content-Type bestimmen
     private static string GetContentType(string url)
     {
         var ext = Path.GetExtension(url).ToLowerInvariant();
         return ext switch
         {
             ".jpg" or ".jpeg" => "image/jpeg",
             ".png" => "image/png",
             ".gif" => "image/gif",
             _ => "application/octet-stream"
         };
     }
}
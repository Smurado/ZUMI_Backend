namespace ZUMI_Backend.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Data;
using Models;
using Models.Enums;
using Models.Maps;
using System.Net.Http.Json;

public static class MedienEndpoints
{
    private static readonly string UploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

    public static void MapBildEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // --- Bilder Upload ---
        // POST /api/v1/projekte/{id:guid}/bilder - Bild hochladen (Owner only)
        endpoints.MapPost("/projekte/{id:guid}/bilder", async (Guid id, IFormFile? file, ApplicationDbContext db, HttpContext http) =>
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
        .Accepts<MultipartFormDataContent>("multipart/form-data");  // Für File-Upload
        
        // --- Medien Upload (Video & Audio) ---
        endpoints.MapPost("/projekte/{id:guid}/medien", async (Guid id, IFormFile? file, ApplicationDbContext db, HttpContext http, IConfiguration config) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // 1. Owner Check
            var isOwner = await db.ProjektPersons.AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();

            // 2. Validierung
            if (file == null || file.Length == 0) return Results.BadRequest("Keine Datei.");
            if (file.Length > 500 * 1024 * 1024) return Results.BadRequest("Datei zu groß (max 500MB).");

            // NEU: Wir prüfen, was es ist
            var isVideo = file.ContentType.StartsWith("video/");
            var isAudio = file.ContentType.StartsWith("audio/");

            // Wenn es weder Video noch Audio ist -> Fehler
            if (!isVideo && !isAudio) return Results.BadRequest("Nur Video- oder Audio-Dateien erlaubt.");

            try
            {
                // Ordner erstellen
                var projektDir = Path.Combine(UploadPath, "projekte", id.ToString());
                Directory.CreateDirectory(projektDir);

                // 3. Original speichern (mit Suffix _ORIGINAL)
                var extension = Path.GetExtension(file.FileName);
                var originalFileName = $"{Guid.NewGuid()}_ORIGINAL{extension}";
                var originalFilePath = Path.Combine(projektDir, originalFileName);

                using (var stream = new FileStream(originalFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // NEU: Ziel-Dateiname definieren
                // Video -> .mp4 (wird zu AV1)
                // Audio -> .mp3 (wird zu MP3)
                var targetExtension = isVideo ? ".mp4" : ".mp3";
                
                // Wir ersetzen die Original-Endung mit der Ziel-Endung
                var finalFileName = originalFileName.Replace("_ORIGINAL", "").Replace(extension, targetExtension);
                var finalRelativeUrl = $"/uploads/projekte/{id}/{finalFileName}";

                // NEU: Richtigen MediaType setzen
                var mediaType = isVideo ? MediaType.Video : MediaType.Audio;

                // 4. DB Eintrag (Status: PENDING)
                var neuesVideo = new Medien
                {
                    ProjektId = id,
                    MediaType = mediaType, // NEU: Dynamisch statt hartkodiert
                    Status = MediaStatus.Pending, 
                    OriginalFileName = originalFileName,
                    Url = finalRelativeUrl 
                };
                db.Medien.Add(neuesVideo);
                await db.SaveChangesAsync();

                // 5. Converter beauftragen
                using var client = new HttpClient();
                var converterUrl = config["ConverterSettings:Url"] ?? "http://converter:8080";
                
                var payload = new 
                { 
                    MediaId = neuesVideo.Id, 
                    InputPath = Path.Combine("projekte", id.ToString(), originalFileName), 
                    // Der Converter schaut auf diese Endung (.mp3 oder .mp4) und entscheidet dann, was er tut!
                    OutputPath = Path.Combine("projekte", id.ToString(), finalFileName)
                };

                try 
                {
                    await client.PostAsJsonAsync($"{converterUrl}/convert", payload);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"WARNUNG: Converter nicht erreichbar! {ex.Message}");
                }

                var resultDto = neuesVideo.MapToMedienDto(); 
                return Results.Created($"/api/v1/videos/{neuesVideo.Id}", resultDto);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Fehler beim Upload: {ex.Message}");
            }
        })
        .DisableAntiforgery()
        .RequireAuthorization()
        .WithName("ErklaerVideoCreate")
        .Accepts<MultipartFormDataContent>("multipart/form-data");        
        
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
        
        endpoints.MapGet("/videos/{videoId:guid}", async (Guid videoId, ApplicationDbContext db, HttpContext http) =>
        {
            // ... Auth & Owner Check
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            var video = await db.Medien
                .Include(m => m.Project).ThenInclude(p => p.Personen)
                .FirstOrDefaultAsync(m => m.Id == videoId);

            if (video == null) return Results.NotFound();

            var isOwner = video.Project.Personen.Any(pp => pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();
            
            string filePath;
            string contentType;
            string downloadName;

            // Fall 1: Konvertierung ist fertig -> Nimm das optimierte AV1 Video
            if (video.Status == MediaStatus.Completed)
            {
                // Pfad aus der URL (die zeigt auf das fertige File)
                var relativePath = video.Url.TrimStart('/').Replace("uploads/", ""); 
                filePath = Path.Combine(UploadPath, relativePath);
                contentType = "video/mp4";
                // Der User bekommt eine .mp4, egal was vorher war
                downloadName = Path.GetFileNameWithoutExtension(video.OriginalFileName) + ".mp4";
            }
            // Fall 2: Noch nicht fertig -> Nimm das Original
            else 
            {
                // Pfad zum Original bauen
                var projektDir = Path.Combine(UploadPath, "projekte", video.ProjektId.ToString());
                filePath = Path.Combine(projektDir, video.OriginalFileName);
                
                // Content-Type erraten (wichtig, damit Browser es versucht abzuspielen)
                contentType = GetContentType(video.OriginalFileName);
                downloadName = video.OriginalFileName;
            }

            if (!File.Exists(filePath)) 
                return Results.NotFound("Datei (noch) nicht verfügbar.");

            // Streaming zurückgeben
            return Results.File(
                path: filePath, 
                contentType: contentType, 
                enableRangeProcessing: true,
                fileDownloadName: downloadName
            );
        })
        .AllowAnonymous()
        .WithName("VideoServe");        

        // DELETE /api/v1/projekte/{id:guid}/medien/{bildId:guid} - Löschen (Owner only)
        // DELETE /api/v1/projekte/{id}/medien/{mediaId} - Löscht Bilder, Videos & Audio komplett
        endpoints.MapDelete("/projekte/{id:guid}/medien/{mediaId:guid}", async (Guid id, Guid mediaId, ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // 1. Owner-Check
            var isOwner = await db.ProjektPersons.AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();

            // 2. Medium laden
            var medium = await db.Medien.FindAsync(mediaId);
            if (medium == null || medium.ProjektId != id) return Results.NotFound();

            // 3. Datei-Bereinigung (Filesystem)
            try 
            {
                // A) Das "Ergebnis"-File löschen (z.B. das Bild, oder das fertige .mp4/.mp3)
                if (!string.IsNullOrEmpty(medium.Url))
                {
                    // Wir entfernen "/uploads/" aus der URL, da UploadPath schon darauf zeigt
                    var relativePath = medium.Url.TrimStart('/').Replace("uploads/", ""); 
                    var finalFilePath = Path.Combine(UploadPath, relativePath);
                    
                    if (File.Exists(finalFilePath)) 
                    {
                        File.Delete(finalFilePath);
                        Console.WriteLine($"[Delete] Datei gelöscht: {finalFilePath}");
                    }
                }

                // B) Das "Original"-File löschen (WICHTIG bei Videos/Audio)
                if (!string.IsNullOrEmpty(medium.OriginalFileName))
                {
                    // Das Original liegt immer im Projekt-Ordner
                    var projectDir = Path.Combine(UploadPath, "projekte", id.ToString());
                    var originalFilePath = Path.Combine(projectDir, medium.OriginalFileName);

                    if (File.Exists(originalFilePath))
                    {
                        File.Delete(originalFilePath);
                        Console.WriteLine($"[Delete] Original gelöscht: {originalFilePath}");
                    }
                }
            }
            catch(Exception ex)
            {
                // Wir loggen den Fehler nur, stoppen aber nicht das Löschen aus der DB.
                // Sonst hat man "Geister-Einträge" in der DB, die man nie loswird.
                Console.WriteLine($"[Error] Fehler beim Löschen der Dateien: {ex.Message}");
            }

            // 4. Aus Datenbank löschen
            db.Medien.Remove(medium);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("MedienDelete") // Neuer Name, da für alle Typen gültig
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
             ".mp4" => "video/mp4",
             ".mp3" => "audio/mpeg",
             ".m4a" => "audio/mp4",
             ".wav" => "audio/wav",
             _ => "application/octet-stream"
         };
     }
}
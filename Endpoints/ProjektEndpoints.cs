
namespace ZUMI_Backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Data;
using Models;
using Models.DTOs;
using Models.Maps;
using Models.Enums;

public static class ProjektEndpoints
{
    public static void MapProjektEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/v1/projekte - Projekt erstellen
        endpoints.MapPost("/projekte/create", async (CreateProjectDto dto, ApplicationDbContext db, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Results.Unauthorized();

                var userId = Guid.Parse(userIdClaim);

                // Neues Project erstellen
                var newProject = new Project
                {
                    Id = Guid.NewGuid(),
                    ProjektStatus = ProjektStatus.InVorbereitung  // Default-Status
                };

                // Essentials mappen
                newProject.ApplyCreateFromDto(dto);

                db.Projekte.Add(newProject);

                // Owner hinzufügen (via Through-Entity)
                db.ProjektPersons.Add(new ProjektPerson
                {
                    ProjektId = newProject.Id,
                    PersonId = userId,
                    IsOwner = true,
                    IsLiked = false,
                    IsParticipating = false  // Defaults; passe an, falls nötig
                });

                await db.SaveChangesAsync();

                // Return DTO (manuell mappen oder via Extension)
                var resultDto = newProject.MapToProjectDto();  // Dein manueller Mapper
                return Results.Created($"/api/v1/projekte/{newProject.Id}", resultDto);
            })
            .RequireAuthorization()
            .WithName("ProjektCreate")
            .WithOpenApi();
        
        endpoints.MapPut("/projekte/{id:guid}/update", async (Guid id, UpdateProjectDto dto, ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            // 1. Berechtigungsprüfung -> User muss Owner vom Projekt sein
            var isOwner = await db.ProjektPersons
                .AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();

            // 2. Laden inkl. Todos (Wichtig für Update/Delete)
            var existingProject = await db.Projekte
                .Include(p => p.Todos)
                .Include(k => k.Kooperationseinrichtungen)
                .Include(m => m.Materialien)
                .Include(b => b.Erklaerbilder)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (existingProject == null) return Results.NotFound();
            
            if (existingProject.Todos == null) existingProject.Todos = new List<Todo>();
            
            // 3. Basis-Projektinfos updaten (Titel, Beschreibung etc.)
            existingProject.ApplyUpdateFromDto(dto);

            // 4. Todos Logik
            if (dto.Todos != null)
            {
                foreach (var todoDto in dto.Todos)
                {
                    // FALL A: Löschen gewünscht?
                    if (todoDto.Delete)
                    {
                        // Nur versuchen zu löschen, wenn wir eine ID haben
                        if (todoDto.Id.HasValue)
                        {
                            // Wir suchen in der geladenen Liste des Projekts
                            var toDelete = existingProject.Todos
                                .FirstOrDefault(t => t.Id == todoDto.Id.Value);

                            if (toDelete != null)
                            {
                                // EF Core merkt das als "Delete" beim SaveChanges
                                existingProject.Todos.Remove(toDelete);
                            }
                        }
                        // Wenn Delete=true, sind wir mit diesem Item fertig -> weiter zum nächsten
                        continue; 
                    }

                    // FALL B: Neu anlegen (Keine ID oder Empty Guid)
                    if (!todoDto.Id.HasValue || todoDto.Id.Value == Guid.Empty)
                    {
                        var newTodo = new Todo
                        {
                            // ID wird im Konstruktor oder hier generiert
                            Id = Guid.NewGuid(), 
                            Titel = todoDto.Titel,
                            Status = todoDto.Status, // Enum (0, 1, 2)
                            Beschreibung = todoDto.Beschreibung,
                            ProjectId = existingProject.Id,
                            //Project = existingProject,
                        }; 

                        // we have to add it to the db first -> will get added to the correct project via id.
                        db.Todos.Add(newTodo);
                    }
                    // FALL C: Update existierendes
                    else
                    {
                        var existingTodo = existingProject.Todos
                            .FirstOrDefault(t => t.Id == todoDto.Id.Value);

                        if (existingTodo != null)
                        {
                            existingTodo.Titel = todoDto.Titel;
                            existingTodo.Status = todoDto.Status;
                            existingTodo.Beschreibung = todoDto.Beschreibung;
                            // Delete Flag ist hier false, also bleibt es bestehen
                        }
                    }
                }
            }
            
            // 5. Kooperationseinrichtung Logik
            if (dto.Kooperationseinrichtungen != null)
            {
                foreach (var kooperationseinrichtungDto in dto.Kooperationseinrichtungen)
                {
                    // FALL A: Neu anlegen (Keine ID oder Empty Guid)
                    if (!kooperationseinrichtungDto.Id.HasValue || kooperationseinrichtungDto.Id.Value == Guid.Empty)
                    {
                        var newkooperationseinrichtung = new Kooperationseinrichtung
                        {
                            // ID wird im Konstruktor oder hier generiert
                            Id = Guid.NewGuid(), 
                            Webseite =  kooperationseinrichtungDto.Webseite,
                            Name = kooperationseinrichtungDto.Name,
                            Projekte = new List<Project>{existingProject},
                            Telefonnummer =  kooperationseinrichtungDto.Telefonnummer,
                            Email = kooperationseinrichtungDto.Email,
                            SocialMedia =  kooperationseinrichtungDto.SocialMedia,
                        }; 

                        // we have to add it to the db first -> will get added to the correct project via id.
                        db.Kooperationseinrichtungen.Add(newkooperationseinrichtung);
                    }
                    // FALL B: Update existierendes Kooperationsstatus
                    else
                    {
                        var existingKooperationseinrichtung = existingProject.Kooperationseinrichtungen
                            .FirstOrDefault(t => t.Id == kooperationseinrichtungDto.Id.Value);

                        if (existingKooperationseinrichtung != null)
                        {
                            existingKooperationseinrichtung.Name = kooperationseinrichtungDto.Name;
                            existingKooperationseinrichtung.Email = kooperationseinrichtungDto.Email;
                            existingKooperationseinrichtung.Webseite = kooperationseinrichtungDto.Webseite;
                            existingKooperationseinrichtung.SocialMedia = kooperationseinrichtungDto.SocialMedia;
                            existingKooperationseinrichtung.Telefonnummer = kooperationseinrichtungDto.Telefonnummer;
                        }
                    }
                }
            }

            if (dto.Materialien != null)
            {
                foreach (var materialDto in dto.Materialien)
                {
                    // FALL A: Löschen
                    if (materialDto.Delete)
                    {
                        if (materialDto.Id.HasValue)
                        {
                            var toDelete = existingProject.Materialien
                                .FirstOrDefault(m => m.Id == materialDto.Id.Value);
                            
                            if (toDelete != null) existingProject.Materialien.Remove(toDelete);  // EF löscht Junction auto
                        }
                    }
                        
                    // FALL B: Neu anlegen
                    if (!materialDto.Id.HasValue || materialDto.Id.Value == Guid.Empty)
                    {
                        var newMaterial = new Material
                        {
                            Id = Guid.NewGuid(),
                            Name = materialDto.Name,
                            Beschreibung = materialDto.Beschreibung,
                            Vorhanden = materialDto.Vorhanden,
                            Projekt = existingProject
                        };

                        db.Materialien.Add(newMaterial);
                        //existingProject.Materialien.Add(newMaterial); 
                    }
                    // FALL C: Update
                    else
                    {
                        var existingMaterial = existingProject.Materialien
                            .FirstOrDefault(m => m.Id == materialDto.Id.Value);

                        if (existingMaterial != null)
                        {
                            existingMaterial.Name = materialDto.Name ?? existingMaterial.Name;
                            existingMaterial.Beschreibung = materialDto.Beschreibung ?? existingMaterial.Beschreibung;
                            existingMaterial.Vorhanden = materialDto.Vorhanden;
                        }
                    }
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(existingProject.MapToProjectDto());
        });
        
        // GET /api/v1/projekte - Alle Projekte (als DTOs)
        endpoints.MapGet("/projekte", async (ApplicationDbContext db) =>
        {
            var projekte = await db.Projekte
                .Include(p => p.Personen)
                    .ThenInclude(pp => pp.Person)
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .Include(p => p.Todos)
                .ToListAsync();

            return projekte.MapToProjectDtos();
        })
        .WithName("ProjektList")
        .WithOpenApi();

        // GET /api/v1/projekte/{id} - Projekt abrufen
        endpoints.MapGet("/projekte/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Personen).ThenInclude(pp => pp.Person)                    
                .Include(p => p.Kooperationseinrichtungen)
                .Include(p => p.Materialien)
                .Include(p => p.Todos)
                .Include(p => p.Erklaerbilder)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (projekt == null) return Results.NotFound();

            var projectDto = projekt.MapToProjectDto();
            
            return Results.Ok(projectDto);
        })
        .WithName("ProjektRetrieve")
        .WithOpenApi();
        
        // GET /api/v1/projekte/{id}/materialien
        endpoints.MapGet("/projekte/{id:guid}/materialien", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (projekt == null) return Results.NotFound();

            var materialDtos = projekt.Materialien.MapToMaterialDtos();
            
            return materialDtos.Count != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialien")
        .WithOpenApi();
        
        // GET /api/v1/projekte/{id}/materialien/gesucht
        endpoints.MapGet("/projekte/{id:guid}/materialien/gesucht", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte
                .Include(p => p.Materialien)
                .FirstOrDefaultAsync(p => p.Id == id);
        
            if (projekt == null) return Results.NotFound();
            
            var materialDtos = projekt.Materialien.MapToMaterialDtos()
                .Where(m => m.Vorhanden == false)
                .ToList();
        
            return materialDtos.Count() != 0 ? Results.Ok(materialDtos) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("ProjektMaterialienGesucht")
        .WithOpenApi();
        
        // A person wants to like, participate or own a project
        // POST /api/v1/projekte/{id}/
        endpoints.MapPost("/projekte/{id:guid}/interaktion", async (ProjektPersonUpdateDto projektPersonUpdateDto, Guid id, ApplicationDbContext db, HttpContext http) =>
        {
            var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            var projekt = await db.Projekte
                .Include(p => p.Personen) .ThenInclude(pp => pp.Person)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            // Check if relationship already exists
            var existingRelation = projekt.Personen.FirstOrDefault(pp => pp.PersonId == userId);
            
            if (projekt == null) return Results.NotFound();
            
            if (existingRelation == null)
            {
                // Create new relation
                var newRelation = new ProjektPerson
                {
                    PersonId = userId,
                    ProjektId = id,
                    IsLiked = projektPersonUpdateDto.IsLiked,
                    IsOwner = projektPersonUpdateDto.IsOwner,
                    IsParticipating = projektPersonUpdateDto.IsParticipating
                };
                db.ProjektPersons.Add(newRelation);
                projekt.Personen.Add(newRelation);
            }
            else
            {
                // Update existing relation
                existingRelation.IsLiked = projektPersonUpdateDto.IsLiked;
                existingRelation.IsOwner = projektPersonUpdateDto.IsOwner;
                existingRelation.IsParticipating = projektPersonUpdateDto.IsParticipating;
            }

            await db.SaveChangesAsync();
            
            // Return the updated project
            var projectDto = projekt.MapToProjectDto();
            return Results.Ok(projectDto);
        })
        .RequireAuthorization()
        .WithName("ProjektUpdateFebe")
        .WithOpenApi();
            
        // GET /api/v1/projekte/sdg/{sdg_id:int} - Projekte gefiltert nach SDG (Enum-Wert 1-17)
        endpoints.MapGet("/projekte/sdg/{sdg_id:int}", async (int sdg_id, ApplicationDbContext db) =>
            {
                if (!Enum.IsDefined(typeof(Sdg), sdg_id))
                    return Results.BadRequest("Ungültiger SDG-Wert (muss 1-17 sein)");

                var projekte = await db.Projekte
                    .Where(p => p.SdgValues.Contains(sdg_id))  // Filter auf List<int> in JSON-Spalte
                    .Include(p => p.ProjektStatus)  // Für Status-Info
                    .Include(p => p.Personen).ThenInclude(pp => pp.Person)  // Für Personen (via Through-Entity)
                    .Include(p => p.Kooperationseinrichtungen)  // Für Kooperationen
                    .Include(p => p.Materialien)  // Für Materialien
                    .Include(p => p.Todos)  // Für Todos (falls im DTO)
                    .Include(p => p.Erklaerbilder)  // Für Erklärbilder (falls im DTO)
                    .ToListAsync();

                return Results.Ok(projekte.MapToProjectDtos());  // Manueller Mapper (aus früherem Chat)
            })
            .AllowAnonymous()
            .WithName("ProjektFilteredBySDG")
            .WithOpenApi();
        
        // DELETE /api/v1/projekte/{id}/delete - Projekt löschen
        endpoints.MapDelete("/projekte/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
        {
            var projekt = await db.Projekte.FindAsync(id);
            if (projekt == null) return Results.NotFound();
            db.Projekte.Remove(projekt);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ProjektDelete")
        .WithOpenApi();

        // GET /api/v1/projekte/startseite
        // Liefert alle Projekte, bei denen der User Owner, Liker oder Mitmacher ist
        // -> nur: ProjektId, Kurztitel, Titelbild, SDG-Ids + Kategorie-Flags
        endpoints.MapGet("/projekte/startseite", async (ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Results.Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);

            var projektPersonen = await db.ProjektPersons
                .AsNoTracking()
                .Where(pp => pp.PersonId == userId &&
                             (pp.IsOwner || pp.IsLiked || pp.IsParticipating))
                .Include(pp => pp.Project)
                .ToListAsync();

            int GetCategory(ProjektPerson pp)
            {
                // Mapping:
                // 0 = Owner, 1 = Liked, 2 = Participating
                if (pp.IsOwner) return 0;
                if (pp.IsLiked) return 1;
                if (pp.IsParticipating) return 2;

                // Fallback, falls mal alle Flags false wären
                return -1;
            }

            var items = projektPersonen.Select(pp => new ProjektStartItemDto
            {
                ProjektId = pp.ProjektId,
                Kurztitel = pp.Project.Kurztitel,
                Titelbild = pp.Project.Titelbild,
                SdgIds = pp.Project.SdgValues,
                Category = GetCategory(pp)
            }).ToList();

            return Results.Ok(items);
        })
        .RequireAuthorization()
        .WithName("ProjektStartseite")
        .WithOpenApi();
        
        // GET /api/v1/projekte/discovery
        // Liefert neue Projekte, an denen der User noch keine Anteilnahme hat (Owner, Liker oder Mitmacher)
        // -> Nur: ProjektId, Kurztitel, Titelbild, SDG-Values (keine Category, da keine Beteiligung)
        // Limit auf 20 aktive Projekte für Performance (erweiterbar mit Query-Params)
        endpoints.MapGet("/projekte/discovery", async (ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // get userId if user is logged in
            var userId = userIdClaim != null ? Guid.Parse(userIdClaim) : (Guid?)null;
            
            IQueryable<Project> query = db.Projekte
                .AsNoTracking()
                .Where(p => p.ProjektStatus != ProjektStatus.Archiviert);
            
            // Hole beteiligte Projekt-IDs (für Exclusion)
            if (userId.HasValue)
            {
                var beteiligteProjektIds = await db.ProjektPersons
                    .AsNoTracking()
                    .Where(pp => pp.PersonId == userId &&
                                 (pp.IsOwner || pp.IsLiked || pp.IsParticipating))
                    .Select(pp => pp.ProjektId)
                    .ToListAsync();
                
                query = query.Where(p => !beteiligteProjektIds.Contains(p.Id));
            }
            
            // Filter nach query
            var projekte = await query
                .OrderBy(p => p.LetztesUpdate)  // Neueste zuerst (optional)
                .Take(20)  // Limit für Discovery (erweiterbar mit ?limit=50)
                .Select(p => new ProjektStartItemDto  // Projiziere direkt zu DTO (effizient)
                {
                    ProjektId = p.Id,
                    Kurztitel = p.Kurztitel,
                    Titelbild = p.Titelbild,
                    SdgIds = p.SdgValues,  // List<int> als SDG-Values
                    Category = -1  // Keine Beteiligung (Fallback)
                })
                .ToListAsync();

            return Results.Ok(projekte);
        })
        .WithName("ProjektDiscovery")
        .WithOpenApi();

        endpoints.MapPut("projekte/{id:guid}/personen/update", async (Guid id, UpdatePersonRolesDto dto,  ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            
            var userId = Guid.Parse(userIdClaim);

            // 1. Berechtigungsprüfung -> User muss Owner vom Projekt sein
            var isOwner = await db.ProjektPersons
                .AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            if (!isOwner) return Results.Forbid();
            
            var existingProject = await db.Projekte
                .Include(p => p.Personen)  // List<ProjektPerson>
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProject == null) return Results.NotFound();

            if (dto.Personen != null)
            {
                foreach (var personUpdate in dto.Personen)
                {
                    // FALL A: Vollständig aus Projekt entfernen
                    if (personUpdate.RemoveFromProject)
                    {
                        var toRemove = existingProject.Personen
                            .FirstOrDefault(pp => pp.PersonId == personUpdate.PersonId);
                        if (toRemove != null)
                        {
                            existingProject.Personen.Remove(toRemove);  // EF trackt als Deleted
                        }
                        continue;
                    }
                    // FALL B: Owner-Rechte setzen (true = machen, false = wegnehmen)
                    var projektPerson = existingProject.Personen
                        .FirstOrDefault(pp => pp.PersonId == personUpdate.PersonId);

                    if (projektPerson == null)
                    {
                        // Wenn Person nicht im Projekt: Neu hinzufügen mit IsOwner
                        projektPerson = new ProjektPerson
                        {
                            ProjektId = id,
                            PersonId = personUpdate.PersonId,
                            IsOwner = personUpdate.IsOwner,
                            IsLiked = false,  // Default
                            IsParticipating = true  // Annahme: Bei Add participating
                        };
                        existingProject.Personen.Add(projektPerson);  // Neu hinzufügen
                    }
                    else
                    {
                        // Bestehend: IsOwner updaten (andere Flags unverändert)
                        projektPerson.IsOwner = personUpdate.IsOwner;
                    }
                }
            }
            // 5. Speichern
            await db.SaveChangesAsync();

            // 6. Response: Updated Project mit Personen
            return Results.Ok(existingProject.MapToProjectDto());
            
        }).RequireAuthorization()
        .WithName("ProjectPersonUpdate")
        .WithOpenApi();
    }
}
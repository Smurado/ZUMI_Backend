namespace ZUMI_Backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Data;
using Models;
using Models.DTOs;
using Models.Maps;
using Models.Enums;
using Models.Helpers;
using Models.ManyToMany;
using Models.Enums.Extensions;

using static Models.Helpers.PermissionHelper;

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
                ProjektStatus = ProjektStatus.InVorbereitung
            };

            // Essentials mappen
            newProject.ApplyCreateFromDto(dto);
            
            db.Projekte.Add(newProject);
            
            // 2. Rollen generieren (Factory)
            // Achtung: CreateDefaultRoles muss jetzt KEINEN "Liker" mehr erzeugen!
            var roles = ProjectRoleFactory.CreateDefaultRoles(newProject.Id, dto.Template); 
            db.ProjectRoles.AddRange(roles);
            
            // 3. Owner verknüpfen
            // Wir geben ihm die "Mitglied" Rolle (oder eine Admin-Rolle aus dem Template)
            var adminRole = roles.FirstOrDefault(r => r.Permissions.HasFlag(ProjectPermissions.ManageMembers)) 
                            ?? roles.FirstOrDefault(r => r.Name == "Mitglied");

            var ownerPerson = new ProjektPerson
            {
                ProjektId = newProject.Id,
                PersonId = userId,
                IsOwner = true,
                IsLiked = false,
                // Neue Logik: Rolle wird über die Liste hinzugefügt
                Roles = new List<ProjektPersonRole>()
            };

            if (adminRole != null)
            {
                ownerPerson.Roles.Add(new ProjektPersonRole
                {
                    ProjectRoleId = adminRole.Id,
                });
            }
            
            db.ProjektPersons.Add(ownerPerson);

            await db.SaveChangesAsync();
            
            // Wir laden das Projekt inkl. Rollen neu für das korrekte Response-DTO
            var createdProject = await db.Projekte
                .Include(p => p.Personen).ThenInclude(pp => pp.Roles).ThenInclude(r => r.ProjectRole)
                .FirstOrDefaultAsync(p => p.Id == newProject.Id);
            
            return Results.Created($"/api/v1/projekte/{newProject.Id}", createdProject.MapToProjectDto());
        })
        .RequireAuthorization()
        .WithName("ProjektCreate")
        .WithOpenApi();
        
        endpoints.MapPut("/projekte/{id:guid}/update", async (Guid id, UpdateProjectDto dto, ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // 2. Laden inkl. Todos (Wichtig für Update/Delete)
            var project = await db.Projekte
                .Include(p => p.Todos)
                .Include(k => k.Kooperationseinrichtungen)
                .Include(m => m.Materialien)
                .Include(b => b.Medien)
                .Include(p => p.Roles)
                .Include(p => p.Personen)
                    .ThenInclude(pp => pp.Roles)
                        .ThenInclude(ppr => ppr.ProjectRole)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (project == null) return Results.NotFound();
            
            var currentUserEntry = project.Personen.FirstOrDefault(pp => pp.PersonId == userId);
            if (currentUserEntry == null) return Results.Forbid();
            
            var permissions = GetCombinedPermissions(currentUserEntry);
            
            // -------------------------------------------------------------------------
            // 3. DIFF-CHECKING: Was ändert sich wirklich?
            // Wir gruppieren die Änderungen nach den neuen Permissions.
            // -------------------------------------------------------------------------

            // GRUPPE A: ManageBasis (Allgemeine Texte & Infos)
            bool isChangingBasis = 
                (dto.Kurztitel != null && dto.Kurztitel != project.Kurztitel) ||
                (dto.Kurzbeschreibung != null && dto.Kurzbeschreibung != project.Kurzbeschreibung) ||
                (dto.Beschreibung != null && dto.Beschreibung != project.Beschreibung) ||
                (dto.Spendeninformationen != null && dto.Spendeninformationen != project.Spendeninformationen) ||
                (dto.WeitereInfos != null && dto.WeitereInfos != project.WeitereInfos) ||
                (dto.SpendenLink != null && dto.SpendenLink != project.SpendenLink) ||
                (dto.SdgValues != null && (project.SdgValues == null || !dto.SdgValues.SequenceEqual(project.SdgValues)));

            // GRUPPE B: ManageLocations (Adressdaten)
            bool isChangingLocation = 
                (dto.Adresse != null && dto.Adresse != project.Adresse) ||
                (dto.Plz != null && dto.Plz != project.Plz) ||
                (dto.StandortLink != null && dto.StandortLink != project.StandortLink);

            // GRUPPE C: ManageTime (Zeiträume)
            bool isChangingTime = 
                (dto.Vorbereitungszeitraum != null && dto.Vorbereitungszeitraum != project.Vorbereitungszeitraum) ||
                (dto.Umsetzungszeitraum != null && dto.Umsetzungszeitraum != project.Umsetzungszeitraum);

            // GRUPPE D: ManageStatus (Projektstatus ändern: In Vorbereitung -> Laufend etc.)
            bool isChangingStatus = 
                dto.Projektstatus.HasValue && 
                dto.Projektstatus != project.ProjektStatus;

            // GRUPPE E: ManageBudget (Harte Zahlen & Finanz-Text)
            bool isChangingBudget = 
                (dto.GesamtBudget.HasValue && dto.GesamtBudget != project.GesamtBudget) ||
                (dto.SpentBudget.HasValue && dto.SpentBudget != project.SpentBudget) ||
                (dto.Finance != null && dto.Finance != project.Finance);

            // GRUPPE F: AddMedia (Titelbild)
            bool isChangingCover = dto.TitelBildId.HasValue && 
                                   project.Medien.FirstOrDefault(m => m.IsCoverPicture)?.Id != dto.TitelBildId.Value;

            // GRUPPE G: Listen (Rollen & Mitglieder) - Hatten wir schon, hier nur der Vollständigkeit halber
            bool isChangingRoles = dto.Rollen != null && dto.Rollen.Any(); 
            bool isChangingMembers = dto.Personen != null && dto.Personen.Any();


            // -------------------------------------------------------------------------
            // 4. RECHTE-GUARD: Zugriff verweigern, wenn Permission fehlt
            // -------------------------------------------------------------------------

            if (isChangingBasis && !permissions.HasFlag(ProjectPermissions.ManageBasis))
                return Results.Json(new { error = "Keine Berechtigung: Basis-Informationen ändern." }, statusCode: 403);

            if (isChangingLocation && !permissions.HasFlag(ProjectPermissions.ManageLocations))
                return Results.Json(new { error = "Keine Berechtigung: Standortdaten ändern." }, statusCode: 403);

            if (isChangingTime && !permissions.HasFlag(ProjectPermissions.ManageTime))
                return Results.Json(new { error = "Keine Berechtigung: Zeiträume ändern." }, statusCode: 403);

            if (isChangingStatus && !permissions.HasFlag(ProjectPermissions.ManageStatus))
                return Results.Json(new { error = "Keine Berechtigung: Projektstatus ändern." }, statusCode: 403);

            if (isChangingBudget && !permissions.HasFlag(ProjectPermissions.ManageBudget))
                return Results.Json(new { error = "Keine Berechtigung: Finanzen/Budget ändern." }, statusCode: 403);

            if (isChangingCover && !permissions.HasFlag(ProjectPermissions.AddMedia)) // Titelbild gehört zu Medien
                return Results.Json(new { error = "Keine Berechtigung: Titelbild ändern." }, statusCode: 403);

            if (isChangingRoles && !permissions.HasFlag(ProjectPermissions.ManageRoles))
                return Results.Json(new { error = "Keine Berechtigung: Rollen verwalten." }, statusCode: 403);

            if (isChangingMembers && !permissions.HasFlag(ProjectPermissions.ManageMembers))
                return Results.Json(new { error = "Keine Berechtigung: Mitglieder verwalten." }, statusCode: 403);
            
            // 5. Daten-Update ausführen
            // Titelbild Logik
            if (isChangingCover)
            {
                foreach (var medium in project.Medien) medium.IsCoverPicture = false;
                var neuesTitelbild = project.Medien.FirstOrDefault(m => m.Id == dto.TitelBildId!.Value);
                if (neuesTitelbild != null) neuesTitelbild.IsCoverPicture = true;
            }
            
            // 6. Basis-Projektinfos updaten (Titel, Beschreibung etc.)
            project.ApplyUpdateFromDto(dto);

            // 7. Todos
            if (dto.Todos != null && dto.Todos.Any())
            {
                if(!permissions.HasFlag(ProjectPermissions.ManageTodos))
                    return Results.Json(new { error = "Keine Berechtigung zum Verwalten von Aufgaben." }, statusCode: 403);
                
                foreach (var todoDto in dto.Todos)
                {
                    // FALL A: Löschen gewünscht?
                    if (todoDto.Delete)
                    {
                        // Nur versuchen zu löschen, wenn wir eine ID haben
                        if (todoDto.Id.HasValue)
                        {
                            // Wir suchen in der geladenen Liste des Projekts
                            var toDelete = project.Todos
                                .FirstOrDefault(t => t.Id == todoDto.Id.Value);

                            if (toDelete != null)
                            {
                                // EF Core merkt das als "Delete" beim SaveChanges
                                project.Todos.Remove(toDelete);
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
                            ProjectId = project.Id,
                            //Project = existingProject,
                        }; 

                        // we have to add it to the db first -> will get added to the correct project via id.
                        db.Todos.Add(newTodo);
                    }
                    // FALL C: Update existierendes
                    else
                    {
                        var existingTodo = project.Todos
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
            
            // 8. Kooperationseinrichtung Logik
            if (dto.Kooperationseinrichtungen != null && dto.Kooperationseinrichtungen.Any())
            {
                if(!permissions.HasFlag(ProjectPermissions.ManageKooperationseinrichtung))
                    return Results.Json(new { error = "Keine Berechtigung zum Verwalten der Kooperationseinrichtung" }, statusCode: 403);

                foreach (var kooperationseinrichtungDto in dto.Kooperationseinrichtungen)
                {
                    // FALL A: Löschen gewünscht?
                    if (kooperationseinrichtungDto.Delete)
                    {
                        // Nur versuchen zu löschen, wenn wir eine ID haben
                        if (kooperationseinrichtungDto.Id.HasValue)
                        {
                            // Wir suchen in der geladenen Liste des Projekts
                            var toDelete = project.Kooperationseinrichtungen
                                .FirstOrDefault(t => t.Id == kooperationseinrichtungDto.Id.Value);

                            if (toDelete != null)
                            { 
                                // EF Core merkt das als "Delete" beim SaveChanges
                                project.Kooperationseinrichtungen.Remove(toDelete);
                            }
                        }
                        // Wenn Delete=true, sind wir mit diesem Item fertig -> weiter zum nächsten
                        continue; 
                    }
                    
                    // FALL A: Neu anlegen (Keine ID oder Empty Guid)
                    if (!kooperationseinrichtungDto.Id.HasValue || kooperationseinrichtungDto.Id.Value == Guid.Empty)
                    {
                        var newkooperationseinrichtung = new Kooperationseinrichtung
                        {
                            // ID wird im Konstruktor oder hier generiert
                            Id = Guid.NewGuid(), 
                            Webseite =  kooperationseinrichtungDto.Webseite,
                            Name = kooperationseinrichtungDto.Name,
                            Projekte = new List<Project>{project},
                            Telefonnummer =  kooperationseinrichtungDto.Telefonnummer,
                            Email = kooperationseinrichtungDto.Email,
                            SocialMedia =  kooperationseinrichtungDto.SocialMedia,
                            Firma =  kooperationseinrichtungDto.Firma,
                        }; 

                        // we have to add it to the db first -> will get added to the correct project via id.
                        db.Kooperationseinrichtungen.Add(newkooperationseinrichtung);
                    }
                    // FALL B: Update existierendes Kooperationsstatus
                    else
                    {
                        var existingKooperationseinrichtung = project.Kooperationseinrichtungen
                            .FirstOrDefault(t => t.Id == kooperationseinrichtungDto.Id.Value);

                        if (existingKooperationseinrichtung != null)
                        {
                            existingKooperationseinrichtung.Name = kooperationseinrichtungDto.Name;
                            existingKooperationseinrichtung.Email = kooperationseinrichtungDto.Email;
                            existingKooperationseinrichtung.Webseite = kooperationseinrichtungDto.Webseite;
                            existingKooperationseinrichtung.SocialMedia = kooperationseinrichtungDto.SocialMedia;
                            existingKooperationseinrichtung.Telefonnummer = kooperationseinrichtungDto.Telefonnummer;
                            existingKooperationseinrichtung.Firma = kooperationseinrichtungDto.Firma;
                        }
                    }
                }
            }

            // 9. Materialien
            if (dto.Materialien != null && dto.Materialien.Any())
            {
             
                if(!permissions.HasFlag(ProjectPermissions.ManageMaterialien))
                    return Results.Json(new { error = "Keine Berechtigung zum Verwalten der Materialien" }, statusCode: 403);
                
                foreach (var materialDto in dto.Materialien)
                {
                    // FALL A: Löschen
                    if (materialDto.Delete)
                    {
                        if (materialDto.Id.HasValue)
                        {
                            var toDelete = project.Materialien
                                .FirstOrDefault(m => m.Id == materialDto.Id.Value);
                            
                            if (toDelete != null) project.Materialien.Remove(toDelete);  // EF löscht Junction auto
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
                            Projekt = project
                        };

                        db.Materialien.Add(newMaterial);
                        //existingProject.Materialien.Add(newMaterial); 
                    }
                    // FALL C: Update
                    else
                    {
                        var existingMaterial = project.Materialien
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
            
            // -----------------------------------------------------------
            // 10. NEU: Rollen-Definitionen (ProjectRole)
            // Wir machen das VOR den Personen, damit neue Rollen existieren.
            // -----------------------------------------------------------
            if (dto.Rollen != null && dto.Rollen.Any())
            {
                // Wir iterieren über das, was vom Frontend kommt
                foreach (var role in dto.Rollen)
                {
                    // FALL A: Löschen
                    if (role.Delete)
                    {
                        if (role.Id.HasValue)
                        {
                            var roleToDelete = project.Roles.FirstOrDefault(r => r.Id == role.Id.Value);
                            if (roleToDelete != null)
                            {
                                if (roleToDelete.IsSystemRole) 
                                    return Results.BadRequest($"Systemrolle '{roleToDelete.Name}' darf nicht gelöscht werden.");
                                
                                // EF Core löscht Kaskadierend die Zuweisungen in ProjektPersonRole, 
                                // wenn in der DB OnDelete Cascade eingestellt ist (Standard).
                                // Falls nicht, müssten wir erst die Zuweisungen löschen.
                                project.Roles.Remove(roleToDelete);
                            }
                        }
                        continue;
                    }

                    // FALL B: Update existierende Rolle
                    if (role.Id.HasValue)
                    {
                        var existingRole = project.Roles.FirstOrDefault(r => r.Id == role.Id.Value);
                        // Wenn Frontend ID schickt, aber wir sie nicht finden -> Ignorieren oder Fehler.
                        // Wir nehmen hier an: Update
                        if (existingRole != null)
                        {
                            existingRole.Name = role.Name;
                            if (role.PermissionPoints.HasValue)
                                existingRole.Permissions = (ProjectPermissions)role.PermissionPoints.Value;
                        }
                        // Sonderfall: Frontend schickt ID (GUID), die es in DB noch nicht gibt (Client-Generated ID für New Role)
                        else 
                        {
                             var newRole = new ProjectRole
                            {
                                Id = role.Id.Value, // Wir nutzen die ID vom Frontend!
                                ProjectId = project.Id,
                                Name = role.Name,
                                Permissions = role.PermissionPoints.HasValue 
                                              ? (ProjectPermissions)role.PermissionPoints.Value 
                                              : ProjectPermissions.None,
                                IsSystemRole = false
                            };
                            project.Roles.Add(newRole);
                        }
                    }
                    
                    // FALL C: Neu ohne ID (Sollte vermieden werden, wenn gleichzeitig zugewiesen wird)
                    else 
                    {
                        var newRole = new ProjectRole
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = project.Id,
                            Name = role.Name,
                            Permissions = role.PermissionPoints.HasValue 
                                          ? (ProjectPermissions)role.PermissionPoints.Value 
                                          : ProjectPermissions.None,
                            IsSystemRole = false
                        };
                        project.Roles.Add(newRole);
                    }
                }
            }
            
            // -----------------------------------------------------------
            // 11. PERSONEN & ROLLEN UPDATE (Integriert)
            // -----------------------------------------------------------
            if (dto.Personen != null && dto.Personen.Any())
            {
                // Permission Check für diesen Abschnitt
                if (!permissions.HasFlag(ProjectPermissions.ManageMembers))
                     return Results.Json(new { error = "Keine Berechtigung zur Mitgliederverwaltung." }, statusCode: 403);

                foreach (var person in dto.Personen)
                {
                    var targetPersonId = person.PersonId;

                    // FALL A: Person entfernen (Delete Flag aus DTO)
                    if (person.Delete)
                    {
                        var toRemove = project.Personen.FirstOrDefault(pp => pp.PersonId == targetPersonId);
                        
                         if (toRemove != null)
                         {
                             if (toRemove.IsOwner)
                             {
                                 // Regel: Nur ein Owner darf einen anderen Owner löschen.
                                 if (!currentUserEntry.IsOwner)
                                     return Results.Json(new { error = "Nur Owner dürfen andere Owner entfernen." }, statusCode: 403);
                                 
                                 // Optionaler Schutz: Verhindern, dass der letzte Owner gelöscht wird
                                 // (Sonst ist das Projekt verwaist)
                                 var ownerCount = project.Personen.Count(p => p.IsOwner);
                                 if (ownerCount <= 1)
                                     return Results.Json(new { error = "Der letzte Owner kann nicht entfernt werden." }, statusCode: 400);
                             }
                             project.Personen.Remove(toRemove);
                         }
                         continue; // Fertig mit dieser Person
                    }

                    // FALL B: Update oder Neu hinzufügen
                    var memberEntry = project.Personen.FirstOrDefault(pp => pp.PersonId == targetPersonId);

                    if (memberEntry == null)
                    {
                        // Neu anlegen
                        memberEntry = new ProjektPerson
                        {
                            ProjektId = id,
                            PersonId = targetPersonId,
                            IsOwner = person.IsOwner, // Standard False
                            IsLiked = false,
                            Roles = new List<ProjektPersonRole>() 
                        };
                        project.Personen.Add(memberEntry);
                    }
                    else
                    {
                        // Update Flags
                        if (person.IsLiked) memberEntry.IsLiked = person.IsLiked;

                        // Owner Check (Nur Owner dürfen Owner ändern)
                        if (person.IsOwner && person.IsOwner != memberEntry.IsOwner)
                        {
                            if (!currentUserEntry.IsOwner) 
                                return Results.Json(new { error = "Nur Owner können den Owner-Status ändern." }, statusCode: 403);
                            
                            memberEntry.IsOwner = person.IsOwner;
                        }
                    }

                    // FALL C: Rollen Synchronisieren (Deine Logik von oben)
                    // Falls die Liste existiert...
                    if (person.Roles != null)
                    {
                        // SCHRITT 1: Wir sammeln alle IDs, die der User haben SOLL (Ziel-Zustand)
                        // Wir gehen davon aus, dass dein ProjectRoleDto ein Feld 'Id' hat.
                        var targetRoleIds = person.Roles.Select(r => r.Id).ToList();

                        // ---------------------------------------------------------
                        // A. Was muss weg? (Cleanup)
                        // ---------------------------------------------------------
                        // Wir suchen Rollen, die der User aktuell hat, die aber NICHT in der neuen Liste stehen.
                        var rolesToRemove = memberEntry.Roles
                            .Where(r => !targetRoleIds.Contains(r.ProjectRoleId))
                            .ToList();

                        foreach (var roleRel in rolesToRemove)
                        {
                            // Löschen aus der Datenbank und der lokalen Liste
                            db.ProjektPersonRoles.Remove(roleRel);
                            memberEntry.Roles.Remove(roleRel);
                        }

                        // ---------------------------------------------------------
                        // B. Was muss dazu? (Dein Code-Snippet)
                        // ---------------------------------------------------------
                        // Wir iterieren über die IDs, die der User haben soll
                        foreach (var roleId in targetRoleIds)
                        {
                            // Check: Hat er die Rolle schon? (Dann müssen wir nichts tun)
                            if (!memberEntry.Roles.Any(r => r.ProjectRoleId == roleId))
                            {
                                // Check: Existiert die Rolle überhaupt im Projekt?
                                // (Wir suchen in der Liste der Projekt-Rollen, die wir oben geladen haben)
                                var roleDefinition = project.Roles.FirstOrDefault(r => r.Id == roleId);
            
                                if (roleDefinition != null)
                                {
                                    // Zuweisung erstellen
                                    memberEntry.Roles.Add(new ProjektPersonRole
                                    {
                                        PersonId = memberEntry.PersonId,
                                        ProjektId = project.Id,
                                        ProjectRoleId = roleId
                                    });
                                }
                            }
                        }
                    }
                }
            }
            
            await db.SaveChangesAsync();

            return Results.Ok(project.MapToProjectDto());
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
                    .AsSplitQuery()
                    .Include(p => p.Personen).ThenInclude(pp => pp.Person)
                    .Include(p =>p.Personen)
                        .ThenInclude(pp => pp.Roles)
                            .ThenInclude(ppr => ppr.ProjectRole)
                    .Include(p => p.Kooperationseinrichtungen)
                    .Include(p => p.Materialien)
                    .Include(p => p.Todos)
                    .Include(p => p.Medien)
                    .Include(p => p.Roles)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (projekt == null) return Results.NotFound();
                
                var projectDto = projekt.MapToProjectDto();

                return Results.Ok(projectDto);
            })
            .WithName("ProjektRetrieve");
        
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
        
        // POST /api/v1/projekte/{id}/interaktion
        // A person wants to like, participate or own a project
        endpoints.MapPost("/projekte/{id:guid}/interaktion", async (ProjektPersonUpdateDto dto, Guid id, ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // 1. Projekt laden inkl. Personen und deren Rollen-Verknüpfungen
            var projekt = await db.Projekte
                .Include(p => p.Personen)
                    .ThenInclude(pp => pp.Roles)
                        .ThenInclude(r => r.ProjectRole)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (projekt == null) return Results.NotFound();
            
            // 2. Bestehende Beziehung suchen
            var existingRelation = projekt.Personen.FirstOrDefault(pp => pp.PersonId == userId);
            
            // 3. Fall: Neue Interaktion (User war noch nie mit dem Projekt verknüpft)
            if (existingRelation == null)
            {
                existingRelation = new ProjektPerson
                {
                    PersonId = userId,
                    ProjektId = id,
                    IsOwner = false, 
                    IsLiked = dto.IsLiked, //
                    Roles = new List<ProjektPersonRole>() // Initial leere Rollenliste
                };

                db.ProjektPersons.Add(existingRelation);
                projekt.Personen.Add(existingRelation);
            }
            else
            {
                // Fall: Update des Like-Status
                existingRelation.IsLiked = dto.IsLiked;
            }

            // 4. Teilnahme-Logik (Rollen-Management)
            // Wir definieren "Mitglied" als die Standard-Rolle für Teilnehmer
            var memberRole = await db.ProjectRoles
                .FirstOrDefaultAsync(r => r.ProjectId == id && r.Name == "Mitglied");

            if (dto.IsParticipating)
            {
                // User will teilnehmen: "Mitglied"-Rolle hinzufügen, falls noch nicht vorhanden
                if (memberRole != null && !existingRelation.Roles.Any(r => r.ProjectRoleId == memberRole.Id))
                {
                    existingRelation.Roles.Add(new ProjektPersonRole
                    {
                        PersonId = userId,
                        ProjektId = id,
                        ProjectRoleId = memberRole.Id //
                    });
                }
            }
            else
            {
                // --- DEINE ANFORDERUNG: Wenn er austritt, fliegen ALLE Rollen raus ---
                if (existingRelation.Roles.Any())
                {
                    
                    // Entfernt alle Rollen-Verknüpfungen aus der Datenbank
                    db.ProjektPersonRoles.RemoveRange(existingRelation.Roles);
            
                    // Leert die lokale Liste für das korrekte Mapping im Response
                    existingRelation.Roles.Clear();
                }
            }

            // 5. Speichern und Ergebnis zurückgeben
            await db.SaveChangesAsync();
            
            // Nutzt den neuen, entschlackten ProjectMapper
            return Results.Ok(projekt.MapToProjectDto());
        })
        .RequireAuthorization()
        .WithName("ProjektInteraktion")
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
                    .Include(p => p.Medien)  // Für Erklärbilder (falls im DTO)
                    .ToListAsync();

                return Results.Ok(projekte.MapToProjectDtos());  // Manueller Mapper (aus früherem Chat)
            })
            .AllowAnonymous()
            .WithName("ProjektFilteredBySDG")
            .WithOpenApi();
        
        // DELETE /api/v1/projekte/{id}/delete - Projekt komplett löschen (inkl. Dateien)
        endpoints.MapDelete("/projekte/{id:guid}/delete", async (Guid id, ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // 1. Owner-Check (Sicherheit: Nur der Besitzer darf löschen!)
            var isOwner = await db.ProjektPersons
                .AnyAsync(pp => pp.ProjektId == id && pp.PersonId == userId && pp.IsOwner);
            
            if (!isOwner) return Results.Forbid();

            var projekt = await db.Projekte.FindAsync(id);
            if (projekt == null) return Results.NotFound();

            // 2. Physische Dateien löschen
            // Wir löschen einfach den ganzen Ordner: /uploads/projekte/{GUID}
            try 
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                var projektDir = Path.Combine(uploadPath, "projekte", id.ToString());

                if (Directory.Exists(projektDir))
                {
                    Directory.Delete(projektDir, recursive: true); // true = Alles darin auch löschen
                    Console.WriteLine($"[Delete] Projekt-Ordner entfernt: {projektDir}");
                }
            }
            catch(Exception ex)
            {
                // Fehler loggen, aber weitermachen, damit das Projekt zumindest aus der DB verschwindet
                Console.WriteLine($"[Error] Konnte Dateien nicht löschen: {ex.Message}");
            }

            // 3. Aus Datenbank entfernen
            // Dank EF Core Cascade Delete werden (meistens) auch alle zugehörigen Todos, Medien & ProjektPersons gelöscht
            db.Projekte.Remove(projekt);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ProjektDelete")
        .WithOpenApi();

        // GET /api/v1/projekte/startseite
        endpoints.MapGet("/projekte/startseite", async (ApplicationDbContext db, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
    
                var userId = Guid.Parse(userIdClaim);

                var projektPersonen = await db.ProjektPersons
                    .AsNoTracking()
                    .Where(pp => pp.PersonId == userId &&
                                 (pp.IsOwner || pp.IsLiked || pp.Roles.Any())) // Fix: pp.ro zu pp.Roles.Any()
                    .Include(pp => pp.Roles)
                    .ThenInclude(projektPersonRole => projektPersonRole.ProjectRole) // Wichtig für die Kategorisierung
                    .Include(pp => pp.Project)
                    .ThenInclude(p => p.Medien)
                    .Include(pp => pp.Person)
                    .ToListAsync();

                // Lokale Hilfsfunktion zur Bestimmung der Anzeige-Kategorie
                int GetCategory(ProjektPerson pp)
                {
                    if (pp.IsOwner) return 0;       // Kategorie: Besitzer
                    if (pp.Roles.Any()) return 2;   // Kategorie: Teilnehmer (hat mindestens eine Rolle)
                    if (pp.IsLiked) return 1;      // Kategorie: Liker

                    return -1;
                }

                var items = projektPersonen.Select(pp => new ProjektStartItemDto
                {
                    ProjektId = pp.ProjektId,
                    Kurztitel = pp.Project.Kurztitel,
                    SdgIds = pp.Project.SdgValues,
                    Category = GetCategory(pp),

                    // Vereinfachte Titelbild-Logik
                    TitelBildId = pp.Project.Medien.FirstOrDefault(m => m.IsCoverPicture)?.Id, 
                    
                    Rollen = pp.Roles.Select(r => new ProjectRoleDto
                    {
                        Id = r.ProjectRole.Id,
                        Name = r.ProjectRole.Name,
                        Permissions = (int)r.ProjectRole.Permissions,
                        IsSystemRole = r.ProjectRole.IsSystemRole
                    }).ToList()
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

                // User-ID ermitteln, falls eingeloggt
                var userId = userIdClaim != null ? Guid.Parse(userIdClaim) : (Guid?)null;

                IQueryable<Project> query = db.Projekte
                    .AsNoTracking()
                    .Where(p => p.ProjektStatus != ProjektStatus.Archiviert);

                // Projekte ausschließen, an denen der User bereits Anteil hat
                if (userId.HasValue)
                {
                    var beteiligteProjektIds = await db.ProjektPersons
                        .AsNoTracking()
                        .Where(pp => pp.PersonId == userId &&
                                     (pp.IsOwner || pp.IsLiked ||
                                      pp.Roles.Any())) // Fix: Roles.Any() statt IsParticipating
                        .Select(pp => pp.ProjektId)
                        .ToListAsync();

                    query = query.Where(p => !beteiligteProjektIds.Contains(p.Id));
                }

                // Ergebnisse abrufen und projizieren
                var projekte = await query
                    .OrderByDescending(p => p.LetztesUpdate) // Neueste Updates zuerst
                    .Take(20)
                    .Select(p => new ProjektStartItemDto
                    {
                        ProjektId = p.Id,
                        Kurztitel = p.Kurztitel,
                        SdgIds = p.SdgValues,
                        Category = -1, // Keine Beteiligung, da Discovery

                        // Effiziente Ermittlung des Titelbildes
                        TitelBildId = p.Medien.FirstOrDefault(m => m.IsCoverPicture).Id
                    })
                    .ToListAsync();

                return Results.Ok(projekte);
            })
            .WithName("ProjektDiscovery")
            .WithOpenApi()
            .AllowAnonymous();

        /*endpoints.MapPut("projekte/{id:guid}/personen/update", async (Guid id, UpdatePersonRolesDto dto,  ApplicationDbContext db, HttpContext http) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();
            
            var userId = Guid.Parse(userIdClaim);

            // 1. Projekt laden inkl. aller Personen und deren Rollen für den Rechtecheck und Sync
            var existingProject = await db.Projekte
                .Include(p => p.Personen)
                    .ThenInclude(pp => pp.Roles)
                        .ThenInclude(r => r.ProjectRole)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProject == null) return Results.NotFound();

            // 2. Berechtigungsprüfung via Permission-Helper
            var currentUserEntry = existingProject.Personen.FirstOrDefault(pp => pp.PersonId == userId);
            if (currentUserEntry == null) return Results.Forbid();

            var permissions = GetCombinedPermissions(currentUserEntry);
            
            // Nur User mit ManageMembers-Recht (oder Owner) dürfen hier Änderungen vornehmen
            if (!permissions.HasFlag(ProjectPermissions.ManageMembers))
            {
                return Results.Json(new { error = "Keine Berechtigung zur Mitgliederverwaltung." }, statusCode: 403);
            }

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
                            existingProject.Personen.Remove(toRemove); 
                        }
                        continue;
                    }
                    
                    // FALL B: Update oder Neu hinzufügen
                    var projektPerson = existingProject.Personen
                        .FirstOrDefault(pp => pp.PersonId == personUpdate.PersonId);
                    
                    if (projektPerson == null)
                    {
                        // Person ist noch nicht im Projekt -> Neu anlegen
                        projektPerson = new ProjektPerson
                        {
                            ProjektId = id,
                            PersonId = personUpdate.PersonId,
                            IsOwner = personUpdate.IsOwner,
                            IsLiked = false, // Admin-Zuweisung setzt kein Like
                            Roles = new List<ProjektPersonRole>() // Initial leer für den Sync unten
                        };
                        existingProject.Personen.Add(projektPerson);
                    }
                    else
                    {
                        // Bestehender User: Owner-Status aktualisieren
                        projektPerson.IsOwner = personUpdate.IsOwner;
                    }

                    // --- ROLLEN SYNCHRONISIEREN (Multi-Role Logic) ---
                    var targetRoleIds = personUpdate.RoleIds ?? new List<Guid>();

                    // 1. Rollen entfernen, die im DTO nicht mehr enthalten sind
                    var rolesToRemove = projektPerson.Roles
                        .Where(r => !targetRoleIds.Contains(r.ProjectRoleId))
                        .ToList();
                    
                    foreach (var roleRel in rolesToRemove)
                    {
                        db.ProjektPersonRoles.Remove(roleRel); // Löschen aus der Join-Tabelle
                        projektPerson.Roles.Remove(roleRel);   // Entfernen aus der lokalen Liste
                    }

                    // 2. Neue Rollen hinzufügen, die der User noch nicht hat
                    foreach (var roleId in targetRoleIds)
                    {
                        if (!projektPerson.Roles.Any(r => r.ProjectRoleId == roleId))
                        {
                            projektPerson.Roles.Add(new ProjektPersonRole
                            {
                                PersonId = projektPerson.PersonId,
                                ProjektId = id,
                                ProjectRoleId = roleId //
                            });
                        }
                    }
                }
            }

            // 3. Speichern aller Änderungen (Person-Links & Rollen-Links)
            await db.SaveChangesAsync();

            // 4. Response via neuem ProjectMapper
            return Results.Ok(existingProject.MapToProjectDto());
            
        }).RequireAuthorization()
        .WithName("ProjectPersonUpdate")
        .WithOpenApi();*/
        
        endpoints.MapGet("/projekte/filter", async ([AsParameters] ProjectSearchFilter filter, ApplicationDbContext db) =>
        {
            // 1. Basis-Abfrage: Nur Projekte mit dem gewünschten Status (1, 2 oder 3)
            var query = db.Projekte
                .AsNoTracking() // Wichtig für Performance
                .Where(p => (int)p.ProjektStatus == filter.Status)
                .AsQueryable();

            // ---------------------------------------------------------
            // SONDERFALL: Standorte (Map)
            // ---------------------------------------------------------
            // Wenn "locations" gefragt ist, geben wir sofort ein schlankes JSON zurück und beenden hier.
            if (filter.Category == ProjectFilterCategory.Locations)
            {
                var locations = await query
                    .Select(p => new 
                    { 
                        p.Id, 
                        p.Kurztitel, 
                        p.Adresse, 
                        p.Plz, 
                        p.SdgValues, 
                        p.StandortLink,
                        // Lat/Lng müsstest du hier ergänzen, falls in DB vorhanden
                    })
                    .Take(100) // Mehr Pins für die Karte erlauben
                    .ToListAsync();
                    
                return Results.Ok(locations);
            }

            // ---------------------------------------------------------
            // FILTER LOGIK (ENUM SWITCH)
            // ---------------------------------------------------------
            // Wir arbeiten auf 'IQueryable<Project>', um Typ-Probleme zu vermeiden.
            IQueryable<Project> q = query;
            
            switch (filter.Category)
            {
                case ProjectFilterCategory.New: // 1
                    // Neueste Projekte zuerst
                    q = q.OrderByDescending(p => p.LetztesUpdate);
                    break;

                case ProjectFilterCategory.Todos: // 2
                    // FALSCH: q = q.Where(p => p.Todos.Any()); 
                    // RICHTIG: Nur Projekte, die mindestens eine OFFENE Aufgabe haben
                    q = q.Where(p => p.Todos.Any(t => (int)t.Status == 0 || (int)t.Status == 1)); // Annahme: 0 = Offen
                    break;

                case ProjectFilterCategory.Materials: // 3
                    // Projekte, die Sachspenden brauchen (nicht vorhandenes Material)
                    q = q.Where(p => p.Materialien.Any(m => !m.Vorhanden));
                    break;

                case ProjectFilterCategory.Financing: // 4
                    // Projekte, die offene Finanzierungsziele haben
                    q = q.Where(p => p.FundingItems.Any(f => f.BereitsGesammelt < f.BenoetigterBetrag)); 
                    break;

                case ProjectFilterCategory.Random: // 5
                    // "Fantastische Arbeit": Zufällig + Nur Projekte mit Bildern
                    q = q.Where(p => p.Medien.Any())
                        .OrderBy(p => Guid.NewGuid());
                    break;

                case ProjectFilterCategory.Discovery: // 0
                default:
                    // Standard: Einfach nach Aktualität
                    q = q.OrderByDescending(p => p.LetztesUpdate);
                    break;
            }

            // ---------------------------------------------------------
            // PAGINATION & DATEN LADEN
            // ---------------------------------------------------------
            var pagedResults = await q
                .Include(p => p.Todos)       // Wichtig für Anzeige "Wir suchen..."
                .Include(p => p.Materialien) // Wichtig für Anzeige "Materialien"
                .Include(p => p.Medien)      // Wichtig für Bilder
                .Include(p => p.FundingItems)
                .Include(p => p.Personen)
                .Skip(filter.Page * filter.Limit) // Überspringen (Seite 0 = 0, Seite 1 = 10...)
                .Take(filter.Limit)               // Nimm die nächsten 10
                .ToListAsync();

            // Mapping auf dein DTO
            var dtos = pagedResults.Select(p => p.MapToProjectDto()).ToList();

            return Results.Ok(dtos);
        })
        .WithName("FilterProjekte");
        
        endpoints.MapGet("/enums/categories", () =>
        {
            var categories = Enum.GetValues<ProjectFilterCategory>()
                .Select(e => new 
                { 
                    Id = (int)e, 
                    Key = e.ToString(), 
                    // Hier nutzen wir jetzt deine existierende Extension-Methode:
                    DisplayName = e.GetDisplayName() 
                });
            
            return Results.Ok(categories);
        })
        .WithName("GetProjectFilterCategories")
        .WithOpenApi(op => new(op) { Summary = "Liefert Kategorien inkl. Display-Name aus EnumExtensions." });
    }
}
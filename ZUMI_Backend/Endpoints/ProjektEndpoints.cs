using Microsoft.AspNetCore.Mvc.Authorization;

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
                ProjektStatus = ProjektStatus.Geplant
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
                .Include(f => f.FundingItems)
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
                (dto.Finance != null && dto.Finance != project.Finance) ||
                (dto.FundingItems != null && dto.FundingItems.Any());

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
            // 9b. Funding Items (Finanzierungsziele)
            // -----------------------------------------------------------
            if (dto.FundingItems != null && dto.FundingItems.Any())
            {
                // Wir nutzen den bereits berechneten Permission-Check von oben (ManageBudget)
                if(!permissions.HasFlag(ProjectPermissions.ManageBudget))
                    return Results.Json(new { error = "Keine Berechtigung: Finanzierungsziele verwalten." }, statusCode: 403);

                foreach (var fundingDto in dto.FundingItems)
                {
                    // FALL A: Löschen
                    if (fundingDto.Delete)
                    {
                        if (fundingDto.Id != Guid.Empty)
                        {
                            var toDelete = project.FundingItems
                                .FirstOrDefault(f => f.Id == fundingDto.Id);
                            
                            if (toDelete != null) 
                            {
                                // EF Core löscht das Item aus der DB
                                project.FundingItems.Remove(toDelete);
                            }
                        }
                        continue;
                    }

                    // FALL B: Neu anlegen (Keine ID oder Empty Guid)
                    if (fundingDto.Id == Guid.Empty || fundingDto.Id == null)
                    {
                        var newItem = new FundingItem
                        {
                            Id = Guid.NewGuid(),
                            Titel = fundingDto.Title,
                            BenoetigterBetrag = fundingDto.BenoetigterBetrag,
                            Beschreibung = fundingDto.Beschreibung,
                            BereitsGesammelt = fundingDto.BereitsGesammelt, 
                            ProjectId = project.Id
                        };

                        // Zur Liste hinzufügen
                        project.FundingItems.Add(newItem);
                        db.FundingItems.Add(newItem); // Zur Sicherheit explizit tracken
                    }
                    // FALL C: Update existierendes Item
                    else
                    {
                        var existingItem = project.FundingItems
                            .FirstOrDefault(f => f.Id == fundingDto.Id);

                        if (existingItem != null)
                        {
                            existingItem.Titel = fundingDto.Title;
                            existingItem.Beschreibung = fundingDto.Beschreibung;
                            existingItem.BenoetigterBetrag = fundingDto.BenoetigterBetrag;
                            
                            // Manuelles Korrigieren des Spendenstandes (falls nötig)
                            existingItem.BereitsGesammelt = fundingDto.BereitsGesammelt;
                        }
                    }
                }
            }
            
            // -----------------------------------------------------------
            // 10. Rollen-Definitionen
            // -----------------------------------------------------------
            if (dto.Rollen != null && dto.Rollen.Any())
            {
                if (!permissions.HasFlag(ProjectPermissions.ManageRoles))
                    return Results.Json(new { error = "Keine Berechtigung: Rollen verwalten." }, statusCode: 403);

                foreach (var roleDto in dto.Rollen)
                {
                    // FALL A: Löschen
                    if (roleDto.Delete)
                    {
                        if (roleDto.Id.HasValue && roleDto.Id.Value != Guid.Empty)
                        {
                            // Wir suchen direkt im Context, um sicherzugehen, dass es getrackt ist
                            var roleToDelete = await db.ProjectRoles.FirstOrDefaultAsync(r => r.Id == roleDto.Id.Value);
                            
                            if (roleToDelete != null)
                            {
                                if (roleToDelete.IsSystemRole)
                                     return Results.BadRequest($"Systemrolle '{roleToDelete.Name}' darf nicht gelöscht werden.");
                                
                                // Explizites Löschen aus dem DB-Set
                                db.ProjectRoles.Remove(roleToDelete);
                            }
                        }
                        continue;
                    }

                    // FALL B: Update oder Neu
                    bool isUpdate = roleDto.Id.HasValue && roleDto.Id.Value != Guid.Empty;

                    if (isUpdate)
                    {
                        // UPDATE: Wir laden die Rolle explizit oder nutzen die aus dem Projekt
                        var existingRole = project.Roles.FirstOrDefault(r => r.Id == roleDto.Id.Value) 
                                           ?? await db.ProjectRoles.FirstOrDefaultAsync(r => r.Id == roleDto.Id.Value);

                        if (existingRole != null)
                        {
                            existingRole.Name = roleDto.Name;
                            if (roleDto.Permissions.HasValue)
                                existingRole.Permissions = (ProjectPermissions)roleDto.Permissions.Value;
                            
                            // Sicherstellen, dass der State auf Modified steht
                            db.Entry(existingRole).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        // NEU: Double-Check auf Namen (Vermeidung von Duplikaten)
                        // Wir prüfen in der lokalen Liste UND in der DB
                        var nameExists = project.Roles.Any(r => r.Name.Equals(roleDto.Name, StringComparison.OrdinalIgnoreCase));
                        
                        if (!nameExists)
                        {
                            var newRole = new ProjectRole
                            {
                                Id = Guid.NewGuid(),
                                ProjectId = project.Id, // Wichtig: Explizite Zuordnung
                                Name = roleDto.Name,
                                Permissions = roleDto.Permissions.HasValue 
                                              ? (ProjectPermissions)roleDto.Permissions.Value 
                                              : ProjectPermissions.None,
                                IsSystemRole = false
                            };
                            
                            // Direktes Hinzufügen zum DbSet ist oft sicherer als zur Collection
                            db.ProjectRoles.Add(newRole);
                            // Wir fügen es auch der lokalen Liste hinzu, damit Abschnitt 11 (unten) die neue Rolle kennt
                            project.Roles.Add(newRole);
                        }
                    }
                }
            }

            // -----------------------------------------------------------
            // 11. PERSONEN & ROLLEN UPDATE
            // -----------------------------------------------------------
            if (dto.Personen != null && dto.Personen.Any())
            {
                if (!permissions.HasFlag(ProjectPermissions.ManageMembers))
                     return Results.Json(new { error = "Keine Berechtigung zur Mitgliederverwaltung." }, statusCode: 403);

                foreach (var person in dto.Personen)
                {
                    var targetPersonId = person.PersonId;

                    // ... (Hier dein Lösch-Code für Personen unverändert lassen) ...
                    if (person.Delete) { /* Dein Delete Code hier... */ continue; }

                    // Person suchen oder anlegen
                    var memberEntry = project.Personen.FirstOrDefault(pp => pp.PersonId == targetPersonId);
                    if (memberEntry == null)
                    {
                        memberEntry = new ProjektPerson { ProjektId = id, PersonId = targetPersonId, Roles = new List<ProjektPersonRole>() };
                        project.Personen.Add(memberEntry);
                    }
                    
                    // Update Properties
                    memberEntry.IsLiked = person.IsLiked;
                    if (person.IsOwner != memberEntry.IsOwner && currentUserEntry.IsOwner) 
                        memberEntry.IsOwner = person.IsOwner;
                    
                    // ROLLEN ZUWEISUNG (Der kritische Teil)
                    if (person.Roles != null)
                    {
                        // SCHRITT 1: Wir "entpacken" die GUIDs aus den Objekten
                        // Annahme: person.Roles ist List<RoleIdWrapper> (oder ähnlich) mit Property .Id
                        var targetRoleIds = person.Roles
                            .Select(r => r.Id)              // <-- Hier greifen wir auf das Property .Id zu
                            .Where(id => id != Guid.Empty)  // <-- Prüfen die GUID, nicht das Objekt
                            .ToList();

                        // A. Cleanup (Was muss weg?)
                        // Wir prüfen gegen die Liste der GUIDs ('targetRoleIds' ist jetzt List<Guid>)
                        var rolesToRemove = memberEntry.Roles
                            .Where(r => !targetRoleIds.Contains(r.ProjectRoleId))
                            .ToList();

                        if (rolesToRemove.Any())
                        {
                            // Entfernen aus DB und lokaler Liste
                            db.ProjektPersonRoles.RemoveRange(rolesToRemove);
                            foreach (var rem in rolesToRemove) memberEntry.Roles.Remove(rem);
                        }

                        // B. Adding (Was muss dazu?)
                        foreach (var roleId in targetRoleIds)
                        {
                            // 'roleId' ist jetzt direkt eine Guid, daher einfacher Vergleich:
                            if (!memberEntry.Roles.Any(r => r.ProjectRoleId == roleId))
                            {
                                // Existiert die Rolle im Projekt?
                                var roleDefinition = project.Roles.FirstOrDefault(r => r.Id == roleId);
            
                                // Sicherheitscheck: Rolle existiert und ist nicht im "Deleted"-Status (EF ChangeTracker)
                                var entry = roleDefinition != null ? db.Entry(roleDefinition) : null;
                                bool isDeleted = entry != null && entry.State == EntityState.Deleted;

                                if (roleDefinition != null && !isDeleted)
                                {
                                    memberEntry.Roles.Add(new ProjektPersonRole
                                    {
                                        PersonId = memberEntry.PersonId,
                                        ProjektId = project.Id,
                                        ProjectRoleId = roleId // <-- Direkt die Guid zuweisen
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
                    .Include(p => p.FundingItems)
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
            
            // -----------------------------------------------------------
            // 4.5 CLEANUP / GARBAGE COLLECTION (Das löst dein Problem)
            // -----------------------------------------------------------
            // Wir prüfen: Hat der Eintrag noch irgendeinen Grund zu existieren?
            // Er muss weg, wenn: KEIN Owner UND KEIN Like UND KEINE Rollen mehr.
    
            // Wichtig: Wir prüfen existingRelation.Roles.Any() -> Das ist dank .Clear() oben korrekt leer.
            bool isZombie = !existingRelation.IsOwner 
                            && !existingRelation.IsLiked 
                            && !existingRelation.Roles.Any();

            if (isZombie)
            {
                // 1. Aus der Datenbank entfernen
                db.ProjektPersons.Remove(existingRelation);
        
                // 2. WICHTIG: Auch aus der lokalen Liste des Projekts entfernen,
                // damit der Mapper unten nicht versucht, den gelöschten Eintrag zurückzugeben!
                projekt.Personen.Remove(existingRelation);
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
        
        endpoints.MapGet("/projekte/filter", async ([AsParameters] ProjectSearchFilter filter, ApplicationDbContext db) =>
        {
            // 1. Basis-Abfrage: Nur Projekte mit dem gewünschten Status (1, 2 oder 3)
            var query = db.Projekte
                .AsNoTracking() // Wichtig für Performance
                .Where(p => (int)p.ProjektStatus == filter.Status)
                .AsQueryable();

            // ---------------------------------------------------------
            // SONDERFALL: Standorte (Map) 6
            // ---------------------------------------------------------
            // Wenn "locations" gefragt ist, geben wir sofort ein schlankes JSON zurück und beenden hier.
            if (filter.Category == ProjectFilterCategory.Locations)
            {
                var locations = await query
                    // FILTER 1: Adresse & PLZ müssen vorhanden sein
                    .Where(p => !string.IsNullOrEmpty(p.Adresse) && !string.IsNullOrEmpty(p.Plz))
            
                    // FILTER 2: OpenStreetMap Link muss existieren (für Lat/Lng Extraction)
                    // Wir prüfen auf null und ob der String "openstreetmap" enthält
                    .Where(p => p.StandortLink != null && p.StandortLink.Contains("openstreetmap"))
            
                    .Select(p => new 
                    { 
                        p.Id, 
                        p.Kurztitel, 
                        p.Adresse, 
                        p.Plz, 
                        p.SdgValues, 
                        p.StandortLink, // Das Frontend extrahiert hieraus Lat/Lng
                        
                        // Wir filtern erst, wählen dann die ID als "Nullable Guid" ((Guid?)) aus 
                        // und nehmen DANN das erste. Wenn keins da ist, kommt null zurück.
                        TitelBildId = p.Medien
                            .Where(m => m.IsCoverPicture)
                            .Select(m => (Guid?)m.Id) // Wichtig: Cast zu Guid?
                            .FirstOrDefault()
                    })
                    .Take(100) // Limit für Map-Pins
                    .ToListAsync();
            
                return Results.Ok(locations);
            }
            
            // ---------------------------------------------------------
            // SONDERFALL: Todos (Aufgaben-Börse) 2
            // ---------------------------------------------------------
            // Wenn "todos" gefragt sind, geben wir direkt eine Liste von Aufgaben zurück
            if (filter.Category == ProjectFilterCategory.Todos)
            {
                var todoList = await db.Todos
                    .AsNoTracking()
                    // 1. Filter: Nur Todos aus Projekten mit dem richtigen Status (z.B. Laufend)
                    .Where(t => (int)t.Project.ProjektStatus == filter.Status)
                    // 2. Filter: Nur offene Aufgaben (0 = Offen, 1 = In Bearbeitung/Besetzt?)
                    // Pass das an deine Logik an. Meistens sucht man hier nur "Offene" (0).
                    .Where(t => (int)t.Status == 0) 
            
                    // 3. Projektion direkt ins DTO (inkl. Projekttitel!)
                    .Select(t => new TodoDto
                    {
                        Id = t.Id,
                        Title = t.Titel,
                        Beschreibung = t.Beschreibung,
                        Status = t.Status,
                        ProjectId = t.ProjectId,
                
                        // Hier holen wir uns den Titel vom Eltern-Projekt
                        ProjectTitle = t.Project.Kurztitel 
                    })
                    // 4. Pagination (Wichtig: Wir paginieren hier Todos, nicht Projekte!)
                    .Skip(filter.Page * filter.Limit)
                    .Take(filter.Limit)
                    .ToListAsync();

                return Results.Ok(todoList);
            }
            
            // ---------------------------------------------------------
            // SONDERFALL: Materials (Sachspenden-Börse) 3
            // ---------------------------------------------------------
            if (filter.Category == ProjectFilterCategory.Materials)
            {
                var materialList = await db.Materialien
                    .AsNoTracking()
                    // 1. Filter: Nur Material aus Projekten mit dem richtigen Status
                    .Where(m => (int)m.Projekt.ProjektStatus == filter.Status)
        
                    // 2. Filter: Nur Dinge, die noch fehlen (!Vorhanden)
                    //    UND die einen Namen haben (keine Leichen)
                    .Where(m => !m.Vorhanden && !string.IsNullOrEmpty(m.Name))

                    // 3. Projektion direkt ins DTO
                    .Select(m => new MaterialDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Beschreibung = m.Beschreibung,
                        Vorhanden = m.Vorhanden,
                        
                        ProjectId = m.ProjektId,       
                        ProjectTitle = m.Projekt.Kurztitel 
                    })
                    // 4. Pagination auf MATERIAL-Ebene (nicht Projekt-Ebene)
                    .Skip(filter.Page * filter.Limit)
                    .Take(filter.Limit)
                    .ToListAsync();

                return Results.Ok(materialList);
            }
            
            // ---------------------------------------------------------
            // SONDERFALL: New (Feed-Ansicht) 1
            // ---------------------------------------------------------
            if (filter.Category == ProjectFilterCategory.New)
            {
                var newProjects = await db.Projekte
                    .AsNoTracking()
                    // Nur Projekte mit dem gewünschten Status (meistens "Laufend" oder alle sichtbaren)
                    .Where(p => (int)p.ProjektStatus == filter.Status)
            
                    // Sortierung: Neueste zuerst
                    // Falls du ein Feld 'ErstelltAm' hast, nimm das. Sonst 'LetztesUpdate'.
                    .OrderByDescending(p => p.LetztesUpdate) 
            
                    .Select(p => new ProjektStartItemDto
                    {
                        ProjektId = p.Id,
                        Kurztitel = p.Kurztitel,
                
                        // Safe Image Logic (wie bei Locations)
                        TitelBildId = p.Medien
                            .Where(m => m.IsCoverPicture)
                            .Select(m => (Guid?)m.Id)
                            .FirstOrDefault(),
                
                        // SDGs direkt übernehmen (EF Core mappt das JSON-Array automatisch)
                        SdgIds = p.SdgValues ?? new List<int>(),
                
                        // Hier musst du entscheiden, was "Category" sein soll.
                        // Im Beispiel war es -1. Ich mappe hier mal den ProjektStatus.
                        // Falls du fest -1 willst: Category = -1,
                        Category = (int)p.ProjektStatus, 
                
                        // Mapping auf CreatedAt (nutzt hier LetztesUpdate als Fallback)
                        CreatedAt = p.LetztesUpdate 
                    })
                    // Pagination
                    .Skip(filter.Page * filter.Limit)
                    .Take(filter.Limit)
                    .ToListAsync();

                return Results.Ok(newProjects);
            }
            
            // ---------------------------------------------------------
            // SONDERFALL: Discovery (Standard Feed / Entdecken)
            // ---------------------------------------------------------
            // Hinweis: Prüfe, ob dein Enum für Discovery wirklich 0 ist
            if (filter.Category == ProjectFilterCategory.Discovery) 
            {
                var discoveryFeed = await db.Projekte
                    .AsNoTracking()
                    .Where(p => (int)p.ProjektStatus == filter.Status)
            
                    // Sortierung: Standard ist meist "Zuletzt aktualisiert"
                    // (Hier könntest du später auch "Random" oder "Algorithmus" einbauen)
                    .OrderByDescending(p => p.LetztesUpdate)
            
                    // Projektion auf das schlanke DTO
                    .Select(p => new ProjektStartItemDto()
                    {
                        ProjektId = p.Id,
                        Kurztitel = p.Kurztitel,
                
                        // Safe Image Logic
                        TitelBildId = p.Medien
                            .Where(m => m.IsCoverPicture)
                            .Select(m => (Guid?)m.Id)
                            .FirstOrDefault(),
                
                        SdgIds = p.SdgValues ?? new List<int>(),
                
                        // Kategorie oder Status mappen
                        Category = (int)p.ProjektStatus, 
                
                        CreatedAt = p.LetztesUpdate
                    })
                    // Pagination
                    .Skip(filter.Page * filter.Limit)
                    .Take(filter.Limit)
                    .ToListAsync();

                return Results.Ok(discoveryFeed);
            }
            
            
            // ---------------------------------------------------------
            // SONDERFALL: Financing (Spenden-Feed) 4
            // ---------------------------------------------------------
            if (filter.Category == ProjectFilterCategory.Financing)
            {
                var fundingList = await db.FundingItems
                    .AsNoTracking()
                    // 1. Filter: Projektstatus muss passen
                    .Where(f => (int)f.Project.ProjektStatus == filter.Status)
        
                    // 2. Filter: Nur Posten, die noch Geld brauchen (noch nicht voll)
                    .Where(f => f.BereitsGesammelt < f.BenoetigterBetrag)

                    // 3. Projektion direkt ins DTO
                    .Select(f => new FundingItemDto
                    {
                        Id = f.Id,
                        Title = f.Titel, // Pass den Property-Namen an, falls er bei dir 'Titel' heißt
                        BenoetigterBetrag = f.BenoetigterBetrag,
                        BereitsGesammelt = f.BereitsGesammelt,
                        ProjectId = f.Project.Id,
                        ProjectTitle = f.Project.Kurztitel
                    })
                    // 4. Pagination auf ITEM-Ebene
                    .Skip(filter.Page * filter.Limit)
                    .Take(filter.Limit)
                    .ToListAsync();

                return Results.Ok(fundingList);
            }
            
            // ---------------------------------------------------------
            // SONDERFALL: Random / Gallery (Bilder-Feed) 5
            // ---------------------------------------------------------
            if (filter.Category == ProjectFilterCategory.Random)
            {
                var mediaList = await db.Medien
                    .AsNoTracking()
                    // 1. Filter: Nur Medien aus Projekten mit dem richtigen Status
                    .Where(m => (int)m.Project.ProjektStatus == filter.Status)
        
                    // 2. Filter: Nur echte Dateien/Bilder (keine leeren Einträge)
                    // Ggf. hier noch auf Dateityp prüfen, falls du auch PDFs hast!
                    // .Where(m => m.Type == MediaType.Image) 
        
                    // 3. Sortierung: ZUFÄLLIG mischen
                    // Hinweis: Bei Pagination und Random kann es passieren, dass Bilder 
                    // doppelt kommen, wenn man scrollt. Für den MVP ist das aber okay.
                    .OrderBy(m => Guid.NewGuid())

                    // 4. Projektion
                    .Select(m => new MedienDto()
                    {
                        Id = m.Id,
                        Url = m.Url, // Oder Url / Blob-Link
                        IsCoverPicture = m.IsCoverPicture,
            
                        // Verknüpfung zum Projekt
                        ProjektId = m.Project.Id,
                        ProjectTitle = m.Project.Kurztitel
                    })
                    // 5. Pagination auf BILD-Ebene
                    .Skip(filter.Page * filter.Limit)
                    .Take(filter.Limit)
                    .ToListAsync();

                return Results.Ok(mediaList);
            }
            
            // ---------------------------------------------------------
            // FILTER LOGIK (ENUM SWITCH)
            // ---------------------------------------------------------
            // Wir arbeiten auf 'IQueryable<Project>', um Typ-Probleme zu vermeiden.
            IQueryable<Project> q = query;
            
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
        .WithName("FilterProjekte")
        .AllowAnonymous();
        
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
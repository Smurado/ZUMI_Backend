using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;
using ZUMI_Backend.Models.DTOs;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// sollte es mal zu einer Kaskadierung durch JSON-Objekte kommen wird bei 64 gebrochen.
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    options.SerializerOptions.MaxDepth = 64; 
});

// JWT-Konfiguration laden (füge zu appsettings.json: "JwtSettings": { "Secret": "...", "Issuer": "...", "Audience": "...", "ExpireDays": 7 })
var jwtConfig = builder.Configuration.GetSection("JwtSettings").Get<JwtConfiguration>() ?? new JwtConfiguration();
builder.Services.AddSingleton(jwtConfig);

// Add services to the container.
// Registriere den DbContext mit PostgreSQL / SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // for postgreSQL
    //options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

    // for sqlite
    options.UseSqlite("Filename=localdev.db");
});

// JWT-Authentication hinzufügen
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(typeof(MappingProfile));

// Konfiguriere Swagger/OpenAPI für API-Docs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()  // Oder spezifisch: .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ... Nach app = builder.Build(); ...

// Aktiviere CORS mit der Policy
app.UseCors("AllowReactFrontend");

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// API-Gruppe unter /api/v1 für Konsistenz mit Django
var api = app.MapGroup("/api/v1");

// --- JWT-Login-Endpoint (POST /api/v1/login) - Passe an deine Person-Auth an
api.MapPost("/login", async (LoginRequest request, ApplicationDbContext db) =>
{
    
    var person = await db.Persons.FirstOrDefaultAsync(p => p.Email == request.Email);
    if (person == null || !BCrypt.Net.BCrypt.Verify(request.Password, person.Password))
    {
        return Results.Unauthorized();
    }

    // Token generieren
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, person.Id.ToString()), // User-ID speichern
        new Claim(ClaimTypes.Name, person.Email ?? "")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtConfig.Issuer,
        audience: jwtConfig.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(jwtConfig.ExpireDays),
        signingCredentials: creds
    );

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
})
.AllowAnonymous()
.WithName("Login")
.WithOpenApi();

// --- Projekt-Endpunkte ---

// POST /api/v1/projekte/ - Projekt erstellen (Auth required)
api.MapPost("/projekte", async (Projekt newProjekt, ApplicationDbContext db, HttpContext http) =>
{
    // User-ID aus Token holen
    var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
    newProjekt.Id = Guid.NewGuid();
    // Füge aktuellen User als verantwortlich hinzu (z.B. via Junction, passe an)
    db.Projekte.Add(newProjekt);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/projekte/{newProjekt.Id}", newProjekt);
})
.RequireAuthorization()
.WithName("ProjektCreate")
.WithOpenApi();

// PUT /api/v1/projekte/{id}/update/ - Projekt aktualisieren (Auth required)
api.MapPut("/projekte/{id:guid}/update", async (Guid id, Projekt updated, ApplicationDbContext db) =>
{
    var existing = await db.Projekte.FindAsync(id);
    if (existing == null) return Results.NotFound();
    // Update Properties (wie zuvor)
    existing.Kurztitel = updated.Kurztitel;
    existing.Kurzbeschreibung = updated.Kurzbeschreibung;
    existing.Titelbild = updated.Titelbild;
    existing.Beschreibung = updated.Beschreibung;
    existing.Vorbereitungszeitraum = updated.Vorbereitungszeitraum;
    existing.Umsetzungszeitraum = updated.Umsetzungszeitraum;
    existing.StandortLink = updated.StandortLink;
    existing.Adresse = updated.Adresse;
    existing.Plz = updated.Plz;
    existing.Spendeninformationen = updated.Spendeninformationen;
    existing.WeitereInfos = updated.WeitereInfos;
    existing.LetztesUpdate = updated.LetztesUpdate;
    existing.ProjektstatusId = updated.ProjektstatusId;

    // Many-to-Many-Beziehungen updaten (Beispiel für Sdgs; wiederhole für andere)
    existing.Sdgs.Clear();
    foreach (var sdg in updated.Sdgs)
    {
        var attachedSdg = await db.SustainableDevelopmentGoals.FindAsync(sdg.Id);
        if (attachedSdg != null)
        {
            existing.Sdgs.Add(attachedSdg);
        }
    }
    // Ähnlich für Personen, Kooperationseinrichtungen, Materialien...

    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ProjektUpdate")
.WithOpenApi();

// GET /api/v1/projekte - Alle Projekte (als DTOs)
api.MapGet("/projekte", async (ApplicationDbContext db, IMapper mapper) =>
{
    var projekte = await db.Projekte
        .Include(p => p.Projektstatus)
        .Include(p => p.Sdgs)
        .Include(p => p.Personen)
        .Include(p => p.Kooperationseinrichtungen)
        .Include(p => p.Materialien)
        .ToListAsync();

    return mapper.Map<List<ProjektDto>>(projekte);
})
.RequireAuthorization()
.WithName("ProjektList")
.WithOpenApi();

// GET /api/v1/projekte/meine - Eigene Projekte (als DTOs)
api.MapGet("/projekte/meine", async (ApplicationDbContext db, HttpContext http, IMapper mapper) =>
    {
        var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
        var projekte = await db.Projekte
            .Where(p => p.Personen.Any(pe => pe.PersonId == userId))
            .Include(p => p.Projektstatus)
            .Include(p => p.Sdgs)
            .Include(p => p.Personen)
            .Include(p => p.Kooperationseinrichtungen)
            .Include(p => p.Materialien)
            .ToListAsync();

        return mapper.Map<List<ProjektDto>>(projekte);  // Map zu DTOs
    })
    .RequireAuthorization()
    .WithName("ProjektMeine")
    .WithOpenApi();

// GET /api/v1/projekte/{id}/ - Projekt abrufen (Auth required)
api.MapGet("/projekte/{id:guid}", async (Guid id, ApplicationDbContext db) =>
{
    var projekt = await db.Projekte
        .Include(p => p.Projektstatus)
        .Include(p => p.Sdgs)
        .Include(p => p.Personen)
        .Include(p => p.Kooperationseinrichtungen)
        .Include(p => p.Materialien)
        .FirstOrDefaultAsync(p => p.Id == id);
    return projekt != null ? Results.Ok(projekt) : Results.NotFound();
})
.RequireAuthorization()
.WithName("ProjektRetrieve")
.WithOpenApi();

// DELETE /api/v1/projekte/{id}/delete/ - Projekt löschen (Auth required)
api.MapDelete("/projekte/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
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

// GET /api/v1/projekte/sdg/{sdg_id}/ - Filtered by SDG (AllowAny? Passe an)
api.MapGet("/projekte/sdg/{sdg_id:int}", async (Guid sdg_id, ApplicationDbContext db) =>
{
    return await db.Projekte
        .Where(p => p.Sdgs.Any(s => s.Id == sdg_id))
        .Include(p => p.Projektstatus)
        .Include(p => p.Sdgs)
        .Include(p => p.Personen)
        .Include(p => p.Kooperationseinrichtungen)
        .Include(p => p.Materialien)
        .ToListAsync();
})
.AllowAnonymous() // Wie in Django
.WithName("ProjektFilteredBySDG")
.WithOpenApi();

// --- Person-Endpunkte (Update own, etc.) ---

// PUT /api/v1/personen/{id}/update/ - Person aktualisieren (Own only, Auth required)
api.MapPut("/personen/{id:guid}/update", async (Guid id, Person updated, ApplicationDbContext db, HttpContext http) =>
{
    var userId = Guid.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
    if (id != userId) return Results.Forbid();
    var existing = await db.Persons.FindAsync(id);
    if (existing == null) return Results.NotFound();
    // Update Properties (passe an deine Person-Fields an)
    // z.B. existing.Name = updated.Name; etc.
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("UpdatePerson")
.WithOpenApi();

// GET /api/v1/personen/ - Alle Personen listen (Auth required, passe bei Bedarf an)
api.MapGet("/personen", async (ApplicationDbContext db) =>
{
    return await db.Persons.ToListAsync();
})
.RequireAuthorization()
.WithName("PersonList")
.WithOpenApi();

// POST /api/v1/personen/ - Person erstellen (AllowAny für Registrierung?)
api.MapPost("/personen", async (Person newPerson, ApplicationDbContext db) =>
{
    if (string.IsNullOrEmpty(newPerson.Password))
    {
        return Results.BadRequest("Passwort ist erforderlich.");
    }
    
    if(string.IsNullOrEmpty(newPerson.Email)) return Results.BadRequest("Email ist erforderlich.");
    
    // Hash das Passwort
    newPerson.Password = BCrypt.Net.BCrypt.HashPassword(newPerson.Password);
    
    db.Persons.Add(newPerson);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/personen/{newPerson.Id}", newPerson);
})
.AllowAnonymous()
.WithName("CreatePerson")
.WithOpenApi();

// --- Weitere Entities: Projektstatus, Rolle, SDG, Kooperationseinrichtung, Materialien ---

// Beispiel für Projektstatus (ähnlich für andere)

// POST /api/v1/projektstatus/ - Create (Auth required)
api.MapPost("/projektstatus", async (Projektstatus newStatus, ApplicationDbContext db) =>
{
    db.Projektstatuses.Add(newStatus);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/projektstatus/{newStatus.Id}", newStatus);
})
.RequireAuthorization()
.WithName("ProjektstatusCreate")
.WithOpenApi();

// GET /api/v1/projektstatus/ - List (AllowAny)
api.MapGet("/projektstatus", async (ApplicationDbContext db) =>
{
    return await db.Projektstatuses.ToListAsync();
})
.AllowAnonymous()
.WithName("ProjektstatusList")
.WithOpenApi();

// GET /api/v1/projektstatus/{id}/ - Retrieve (AllowAny)
api.MapGet("/projektstatus/{id:guid}", async (Guid id, ApplicationDbContext db) =>
{
    var status = await db.Projektstatuses.FindAsync(id);
    return status != null ? Results.Ok(status) : Results.NotFound();
})
.AllowAnonymous()
.WithName("ProjektstatusRetrieve")
.WithOpenApi();

// PUT /api/v1/projektstatus/{id}/update/ - Update (Auth required)
api.MapPut("/projektstatus/{id:guid}/update", async (Guid id, Projektstatus updated, ApplicationDbContext db) =>
{
    var existing = await db.Projektstatuses.FindAsync(id);
    if (existing == null) return Results.NotFound();
    // Update Properties
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ProjektstatusUpdate")
.WithOpenApi();

// DELETE /api/v1/projektstatus/{id}/delete/ - Delete (Auth required)
api.MapDelete("/projektstatus/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
{
    var status = await db.Projektstatuses.FindAsync(id);
    if (status == null) return Results.NotFound();
    db.Projektstatuses.Remove(status);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ProjektstatusDelete")
.WithOpenApi();

// Wiederhole das Pattern für Rolle, SustainableDevelopmentGoal, Kooperationseinrichtung, Materialien (z.B. MapGroup für jede)

// POST /api/v1/todos/create/
api.MapPost("/todos/create", async (Todo newTodo, ApplicationDbContext db) =>
{
    db.Todos.Add(newTodo);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/todos/{newTodo.Id}", newTodo);
})
.RequireAuthorization()
.WithName("TodoCreate")
.WithOpenApi();

// GET /api/v1/todos/{id}/
api.MapGet("/todos/{id:guid}", async (Guid id, ApplicationDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    return todo != null ? Results.Ok(todo) : Results.NotFound();
})
.RequireAuthorization()
.WithName("TodoRetrieve")
.WithOpenApi();

// PUT /api/v1/todos/{id}/update/
api.MapPut("/todos/{id:guid}/update", async (Guid id, Todo updated, ApplicationDbContext db) =>
{
    var existing = await db.Todos.FindAsync(id);
    if (existing == null) return Results.NotFound();
    // Update Properties
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("TodoUpdate")
.WithOpenApi();

// DELETE /api/v1/todos/{id}/delete/
api.MapDelete("/todos/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo == null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("TodoDelete")
.WithOpenApi();

// GET /api/v1/projekte/{projekt_id}/todos/ - Todos für Projekt listen (Auth required)
api.MapGet("/projekte/{projekt_id:guid}/todos", async (Guid projekt_id, ApplicationDbContext db) =>
{
    return await db.Todos.Where(t => t.ProjektId == projekt_id).ToListAsync();
})
.RequireAuthorization()
.WithName("ProjektTodosList")
.WithOpenApi();

// DELETE /api/v1/erklaerbilder/{id}/delete/ - Delete
api.MapDelete("/erklaerbilder/{id:guid}/delete", async (Guid id, ApplicationDbContext db) =>
{
    var bild = await db.Erklaerbilder.FindAsync(id);
    if (bild == null) return Results.NotFound();
    db.Erklaerbilder.Remove(bild);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ErklaerbildDelete")
.WithOpenApi();

// GET /api/v1/projekte/{projekt_id}/erklaerbilder/ - List for Project
api.MapGet("/projekte/{projekt_id:guid}/erklaerbilder", async (Guid projekt_id, ApplicationDbContext db) =>
{
    return await db.Erklaerbilder.Where(e => e.ProjektId == projekt_id).ToListAsync(); // Passe an dein Model
})
.RequireAuthorization()
.WithName("ErklaerbildList")
.WithOpenApi();

// --- API Root: GET /api/v1/ - Übersicht mit Links ---
api.MapGet("/", (HttpRequest req) =>
{
    return new
    {
        projekte = $"{req.Scheme}://{req.Host}/api/v1/projekte",
        meine_projekte = $"{req.Scheme}://{req.Host}/api/v1/projekte/meine",
        projekt_create = $"{req.Scheme}://{req.Host}/api/v1/projekte",
        projekt_retrieve = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}",
        projekt_update = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}/update",
        projekt_delete = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}/delete",
        person_projekt = $"{req.Scheme}://{req.Host}/api/v1/person_projekt", // Passe an, falls implementiert
        personen = $"{req.Scheme}://{req.Host}/api/v1/personen",
        create_person = $"{req.Scheme}://{req.Host}/api/v1/personen",
        update_person = $"{req.Scheme}://{req.Host}/api/v1/personen/{{id}}/update",
        projektstatus = $"{req.Scheme}://{req.Host}/api/v1/projektstatus",
        rolle = $"{req.Scheme}://{req.Host}/api/v1/rolle", // Füge Endpoints hinzu
        sdg = $"{req.Scheme}://{req.Host}/api/v1/sdg",
        kooperationspartner = $"{req.Scheme}://{req.Host}/api/v1/kooperationspartner",
        materialien = $"{req.Scheme}://{req.Host}/api/v1/materialien",
        projekt_filtered_by_sdg = $"{req.Scheme}://{req.Host}/api/v1/projekte/sdg/{{sdg_id}}",
        projektinfo = $"{req.Scheme}://{req.Host}/api/v1/projektinfo", // Füge bei Bedarf
        token_obtain_pair = $"{req.Scheme}://{req.Host}/api/v1/login", // Unser Login
        token_refresh = $"{req.Scheme}://{req.Host}/api/v1/token/refresh", // Implementiere bei Bedarf
        token_logout = $"{req.Scheme}://{req.Host}/api/v1/token/logout", // Implementiere bei Bedarf
        api_root = $"{req.Scheme}://{req.Host}/api/v1/"
    };
})
.AllowAnonymous()
.WithName("ApiRoot")
.WithOpenApi();

app.Run();
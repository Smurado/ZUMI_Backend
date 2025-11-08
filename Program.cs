using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using ZUMI_Backend.Data;
using ZUMI_Backend.Endpoints;
using ZUMI_Backend.Models;

var builder = WebApplication.CreateBuilder(args);

// JSON-Options konfigurieren
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    options.SerializerOptions.MaxDepth = 64;
});

// JWT-Konfiguration laden
var jwtConfig = builder.Configuration.GetSection("JwtSettings").Get<JwtConfiguration>() ?? new JwtConfiguration();
builder.Services.AddSingleton(jwtConfig);

// DbContext registrieren (SQLite für Dev)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite("Filename=localdev.db");  // Oder UseNpgsql für PostgreSQL
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

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// CORS, Auth, etc.
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Seed Data
/*using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}*/

// Dev-Features
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// API-Gruppe
var api = app.MapGroup("/api/v1");

// Registriere Endpunkte via Extensions
api.MapAuthEndpoints();
api.MapProjektEndpoints();
api.MapPersonEndpoints();
api.MapProjektstatusEndpoints();
api.MapRolleEndpoints();
api.MapKooperationseinrichtungEndpoints();
api.MapMaterialienEndpoints();
api.MapSdgEndpoints();
// Füge hier weitere hinzu, z.B. api.MapRolleEndpoints(); usw.
api.MapTodoEndpoints();
api.MapErklaerbildEndpoints();
api.MapApiRootEndpoints();

app.Run();
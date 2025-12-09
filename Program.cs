using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ZUMI_Backend.Data;
using ZUMI_Backend.Endpoints;
using ZUMI_Backend.Endpoints.InternalEndpoints;
using ZUMI_Backend.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// 1. Kestrel Server Limit erhöhen (Server-Level Limit)
builder.WebHost.ConfigureKestrel(options =>
{
    // 1 GB Limit (in Bytes: 1024 * 1024 * 1024 = 1073741824)
    options.Limits.MaxRequestBodySize = 1073741824; 
});

// 2. Formular Limit erhöhen (für IFormFile Verarbeitung in ASP.NET)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1073741824; // 1 GB
});

// JSON-Options konfigurieren
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    options.SerializerOptions.MaxDepth = 64;
});

// JWT-Konfiguration laden
var jwtConfig = builder.Configuration.GetSection("JwtSettings").Get<JwtConfiguration>() ?? new JwtConfiguration();
builder.Services.AddSingleton(jwtConfig);

// DbContext registrieren
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
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

builder.Services.AddHttpClient();

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

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";  // Für AJAX/Frontend-Requests
    options.SuppressXFrameOptionsHeader = false;  // Falls iFrames nötig
});

var app = builder.Build();

app.UseHttpsRedirection();
    
// Nur für andere StaticFiles, nicht uploads
app.UseStaticFiles(new StaticFileOptions
{
  RequestPath = "",  // Oder spezifisch: FileProvider für andere Ordner
  // Kein Serve für /uploads – blockt direkten Access
});

// CORS, Auth, etc.
app.UseCors();

app.UseRouting();

app.UseAuthentication();
app.UseAntiforgery(); // Fix: Ermöglicht CSRF-Token-Handling für Forms
app.UseAuthorization();


app.UseRouting();

// Seed Data
/*using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}*/

// Dev-Features
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

// API-Gruppe
var api = app.MapGroup("/api/v1");

// Registriere Endpunkte via Extensions
api.MapAuthEndpoints();
api.MapProjektEndpoints();
api.MapPersonEndpoints();
api.MapProjektstatusEndpoints();
api.MapKooperationseinrichtungEndpoints();
api.MapMaterialienEndpoints();
api.MapSdgEndpoints();
api.MapTodoEndpoints();
api.MapBildEndpoints();
api.MapApiRootEndpoints();
api.MapInternalEndpoints();
api.MapFeedbackEndpoints();
api.MapAltersgruppeEndpoints();

app.Run();
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
builder.Services.AddAutoMapper(cfg => {
    cfg.LicenseKey = "<eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzk0MDk2MDAwIiwiaWF0IjoiMTc2MjYyNzQ4MyIsImFjY291bnRfaWQiOiIwMTlhNjRjOGIwYWY3YjliOGNlMGQyYmQzZjg2ODY5MyIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazlqY2h4amJybWRzOWVwZXljajBlOGtuIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.TRUquj0PoiDe1F8laoWpR8AkYmdDwRObB3dCpyeU-TI1WzHa0vqFu41JQij9knsmlAQKM4XzrM26qJrwou-drrj5nAIewESC67bkl-TJmLR6ND8R0TPncJcwLm2mYDn2LjaEJSrfEzcoPu0LdD5MG1V7fCnGEzi1dNSVfxquNFc3sYTceoiAKP2Tum531CgeV0VmbEeZ68nXqp3cmc606ep28LJlDWw3madA4mlfT7IjVlKF9sgLxmD8D_MDS7qnCRFk53FNo1CIW23u8IIK4lrg3DOrSWdlWrZ6WEaLGBJil_7C8jcprrGBt-D2k-Xo55tGZW4tbiHjwykB3662QQ>";
}, typeof(MappingProfile));

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
api.MapRolleEndpoints();
api.MapKooperationseinrichtungEndpoints();
api.MapMaterialienEndpoints();
api.MapSdgEndpoints();
api.MapTodoEndpoints();
api.MapBildEndpoints();
api.MapApiRootEndpoints();
api.MapFeedbackEndpoints();
api.MapAltersgruppeEndpoints();

app.Run();
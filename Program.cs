using Microsoft.EntityFrameworkCore;
using ZUMI_Backend.Data;  // Dein Namespace für ApplicationDbContext

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Registriere den DbContext mit PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Aktiviere Controllers (für REST-Endpunkte)
builder.Services.AddControllers();

// Konfiguriere Swagger/OpenAPI für API-Docs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Optional: CORS für React-Frontend (passe Origins an)
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

// Aktiviere CORS
app.UseCors();

// Map Controllers (für deine zukünftigen Endpoints)
app.MapControllers();

app.Run();
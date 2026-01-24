/*namespace ZUMI.Tests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Xunit;
using ZUMI_Backend.Data; // Namespace deines DbContext

// IAsyncLifetime garantiert, dass der Docker-Container VOR den Tests startet
public class ConverterApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Wir starten einen echten Postgres 16 Container für die Tests
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1. Den echten VideoProcessorWorker entfernen (stört im API Test)
            // Passe 'VideoProcessorWorker' an, falls der Namespace explizit benötigt wird
            var worker = services.FirstOrDefault(d => d.ImplementationType?.Name == "VideoProcessorWorker"); 
            if (worker != null) services.Remove(worker);

            // 2. Die echte Datenbank-Konfiguration entfernen
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // 3. Testcontainers-Datenbank verbinden
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // 4. Authentifizierung austauschen (Mock Auth)
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, MockAuthHandler>("Test", options => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        // Datenbank-Schema erstellen (damit Tabellen existieren)
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}*/
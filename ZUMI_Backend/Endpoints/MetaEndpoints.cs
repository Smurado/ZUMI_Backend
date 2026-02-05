namespace ZUMI_Backend.Endpoints;

public static class MetaEndpoints
{
    public static void MapMetaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // ---------------------------------------------------------
        // Partner Logos (Aus dem wwwroot Ordner)
        // ---------------------------------------------------------
        endpoints.MapGet("/meta/partners", (IWebHostEnvironment env, HttpRequest request) =>
        {
            var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folderPath = Path.Combine(webRoot, "images", "partners");

            if (!Directory.Exists(folderPath))
            {
                return Results.Ok(new List<object>());
            }

            // Wir fragen den Header ab, den Nginx mitschickt
            var forwardedProto = request.Headers["X-Forwarded-Proto"].FirstOrDefault();

            // Logik:
            // 1. Wenn Nginx sagt "es war https", nehmen wir "https".
            // 2. Wenn der Header fehlt (z.B. beim lokalen Testen ohne Nginx), nehmen wir das, was .NET sieht (request.Scheme).
            var scheme = forwardedProto ?? request.Scheme;

            var baseUrl = $"{scheme}://{request.Host}";

            var logos = Directory.GetFiles(folderPath)
                .Select(path => Path.GetFileName(path))
                .Where(name => name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                               name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                               name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || 
                               name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                .Select(fileName => new 
                {
                    Name = fileName,
                    Url = $"{baseUrl}/images/partners/{fileName}"
                })
                .ToList();

            return Results.Ok(logos);
        })
        .WithName("GetPartnerLogos")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Liefert URLs zu Partner-Logos",
            Description = "Scannt den Ordner wwwroot/images/partners und gibt alle gefundenen Bilder zurück."
        });
    }
}
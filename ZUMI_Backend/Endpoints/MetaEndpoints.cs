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

            var baseUrl = $"{request.Scheme}://{request.Host}";

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
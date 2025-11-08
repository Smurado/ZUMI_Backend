using Microsoft.AspNetCore.Builder;

namespace ZUMI_Backend.Endpoints;

public static class ApiRootEndpoints
{
    public static void MapApiRootEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/v1/ - Übersicht mit Links
        endpoints.MapGet("/", (HttpRequest req) =>
        {
            return new
            {
                // Token
                token_obtain_pair = $"{req.Scheme}://{req.Host}/api/v1/auth/token/",
                token_refresh = $"{req.Scheme}://{req.Host}/api/v1/token/refresh/",
                token_logout = $"{req.Scheme}://{req.Host}/api/v1/token/logout/",
                
                // Projekte
                projekte = $"{req.Scheme}://{req.Host}/api/v1/projekte",
                meine_projekte = $"{req.Scheme}://{req.Host}/api/v1/projekte/meine",
                projekt_create = $"{req.Scheme}://{req.Host}/api/v1/projekte/create",
                projekt_retrieve = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}",
                projekt_update = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}/update",
                projekt_delete = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}/delete",
                projektinfo = $"{req.Scheme}://{req.Host}/api/v1/projektinfo",
                
                // Person
                personen = $"{req.Scheme}://{req.Host}/api/v1/personen",
                create_person = $"{req.Scheme}://{req.Host}/api/v1/personen",
                update_person = $"{req.Scheme}://{req.Host}/api/v1/personen/{{id}}/update",
                
                // Materialien
                materialien = $"{req.Scheme}://{req.Host}/api/v1/materialien",
                
                // SDG
                sdg = $"{req.Scheme}://{req.Host}/api/v1/sdg",
                
                // Kooperationseinrichtung
                kooperationspartner = $"{req.Scheme}://{req.Host}/api/v1/kooperationspartner",
                
                // Todos
                
                
                // Erklaerbilder
                
                person_projekt = $"{req.Scheme}://{req.Host}/api/v1/person_projekt",
                projektstatus = $"{req.Scheme}://{req.Host}/api/v1/projektstatus",
                rolle = $"{req.Scheme}://{req.Host}/api/v1/rolle",
                projekt_filtered_by_sdg = $"{req.Scheme}://{req.Host}/api/v1/projekte/sdg/{{sdg_id}}",
                api_root = $"{req.Scheme}://{req.Host}/api/v1/"
            };
        })
        .AllowAnonymous()
        .WithName("ApiRoot")
        .WithOpenApi();
    }
}
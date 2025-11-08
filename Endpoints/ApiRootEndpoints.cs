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
                projekte = $"{req.Scheme}://{req.Host}/api/v1/projekte",
                meine_projekte = $"{req.Scheme}://{req.Host}/api/v1/projekte/meine",
                projekt_create = $"{req.Scheme}://{req.Host}/api/v1/projekte",
                projekt_retrieve = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}",
                projekt_update = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}/update",
                projekt_delete = $"{req.Scheme}://{req.Host}/api/v1/projekte/{{id}}/delete",
                person_projekt = $"{req.Scheme}://{req.Host}/api/v1/person_projekt", // Passe an
                personen = $"{req.Scheme}://{req.Host}/api/v1/personen",
                create_person = $"{req.Scheme}://{req.Host}/api/v1/personen",
                update_person = $"{req.Scheme}://{req.Host}/api/v1/personen/{{id}}/update",
                projektstatus = $"{req.Scheme}://{req.Host}/api/v1/projektstatus",
                rolle = $"{req.Scheme}://{req.Host}/api/v1/rolle",
                sdg = $"{req.Scheme}://{req.Host}/api/v1/sdg",
                kooperationspartner = $"{req.Scheme}://{req.Host}/api/v1/kooperationspartner",
                materialien = $"{req.Scheme}://{req.Host}/api/v1/materialien",
                projekt_filtered_by_sdg = $"{req.Scheme}://{req.Host}/api/v1/projekte/sdg/{{sdg_id}}",
                projektinfo = $"{req.Scheme}://{req.Host}/api/v1/projektinfo",
                token_obtain_pair = $"{req.Scheme}://{req.Host}/api/v1/login",
                token_refresh = $"{req.Scheme}://{req.Host}/api/v1/token/refresh",
                token_logout = $"{req.Scheme}://{req.Host}/api/v1/token/logout",
                api_root = $"{req.Scheme}://{req.Host}/api/v1/"
            };
        })
        .AllowAnonymous()
        .WithName("ApiRoot")
        .WithOpenApi();
    }
}
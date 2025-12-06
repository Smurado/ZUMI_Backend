namespace ZUMI_Backend.Endpoints;
using Models.Enums;
using Models.Maps;

public static class ProjektstatusEndpoints
{
    public static void MapProjektstatusEndpoints(this IEndpointRouteBuilder endpoints)
    {

        // GET /api/v1/projektstatus - List
        endpoints.MapGet("/projektstatus", () =>
        {
            var states = Enum.GetValues<ProjektStatus>()
                .Select(status => status.MapToProjektstatusDto())
                .ToList();
            return Results.Ok(states);
        })
        .AllowAnonymous()
        .WithName("ProjektstatusList")
        .WithOpenApi();

        // GET /api/v1/projektstatus/{id} - Retrieve
        endpoints.MapGet("/projektstatus/{id:int}", (int id) =>
            {
                if (!Enum.IsDefined(typeof(ProjektStatus), id))
                    return Results.NotFound("Projektstatus nicht gefunden");

                var status = (ProjektStatus)id;
                return Results.Ok(status.MapToProjektstatusDto());
            })
            .AllowAnonymous()
            .WithName("ProjektstatusRetrieve")
            .WithOpenApi();
    }
} 
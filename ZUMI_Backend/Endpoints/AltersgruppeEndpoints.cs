using ZUMI_Backend.Models.Maps;

namespace ZUMI_Backend.Endpoints;

using Models.Enums;

public static class AltersgruppeEndpoints
{
    public static void MapAltersgruppeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/v1/altersgruppe - List
        endpoints.MapGet("/altersgruppe", () =>
            {
                var altersgruppen = Enum.GetValues<Altersgruppe>()
                    .Select(ag => ag.MapToAltersgruppeDto())
                    .ToList();
                return Results.Ok(altersgruppen);
            })
            .AllowAnonymous()
            .WithName("AltersgruppeList")
            .WithOpenApi();

        // GET /api/v1/altersgruppe/{id:int} - Retrieve Altersgruppe by Enum-Wert (0-4)
        endpoints.MapGet("/altersgruppe/{id:int}", (int id) =>
            {
                if (!Enum.IsDefined(typeof(Altersgruppe), id))
                    return Results.NotFound("Altersgruppe nicht gefunden");

                var ag = (Altersgruppe)id;
                return Results.Ok(ag.MapToAltersgruppeDto());
            })
            .AllowAnonymous()
            .WithName("AltersgruppeRetrieve")
            .WithOpenApi();
    }
}
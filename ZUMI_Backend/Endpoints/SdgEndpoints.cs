using ZUMI_Backend.Models.Maps;

namespace ZUMI_Backend.Endpoints;

using Data;
using Models.DTOs;
using Models.Enums;

public static class SdgEndpoints
{
    public static void MapSdgEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/v1/sdg - List
        endpoints.MapGet("/sdg", () =>
        {
            var sdgs = Enum.GetValues<Sdg>()
                .Select(sdg => sdg.MapToSdgDto())
                .ToList();
            return Results.Ok(sdgs);
        })
        .AllowAnonymous()
        .WithName("SdgList")
        .WithOpenApi();

        // GET /api/v1/sdg/{id:int} - Retrieve SDG by Enum-Wert (1-17)
        endpoints.MapGet("/sdg/{id:int}", (int id) =>
        {
            if (!Enum.IsDefined(typeof(Sdg), id))
                return Results.NotFound("SDG nicht gefunden");

            var sdg = (Sdg)id;
            return Results.Ok(sdg.MapToSdgDto());
        })
        .AllowAnonymous()
        .WithName("SdgRetrieve")
        .WithOpenApi();
    }
} 
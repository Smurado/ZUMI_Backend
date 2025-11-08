using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ZUMI_Backend.Data;
using ZUMI_Backend.Models;

namespace ZUMI_Backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // JWT-Login-Endpoint (POST /api/v1/login)
        endpoints.MapPost("/login", async (LoginRequest request, ApplicationDbContext db, JwtConfiguration jwtConfig) =>
            {
                var person = await db.Persons.FirstOrDefaultAsync(p => p.Email == request.Email);
                if (person == null || !BCrypt.Net.BCrypt.Verify(request.Password, person.Password))
                {
                    return Results.Unauthorized();
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, person.Id.ToString()),
                    new Claim(ClaimTypes.Name, person.Email ?? "")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtConfig.Issuer,
                    audience: jwtConfig.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(jwtConfig.ExpireDays),
                    signingCredentials: creds
                );

                return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            })
            .AllowAnonymous()
            .WithName("Login")
            .WithOpenApi();
    }
}
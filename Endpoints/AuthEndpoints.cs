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
        var authGroup = endpoints.MapGroup("/auth");

        // JWT-Login-Endpoint (POST /api/v1/auth/token/) -> Returns access and refresh tokens, stores refresh in OutstandingTokens
        authGroup.MapPost("/token/", async (LoginRequest request, ApplicationDbContext db, JwtConfiguration jwtConfig) =>
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

            // Generate access token (short-lived)
            var accessToken = GenerateToken(claims, jwtConfig, TimeSpan.FromMinutes(60), out string accessJti); // 15 min expiry

            // Generate refresh token (long-lived) and store in DB
            var refreshToken = GenerateToken(claims, jwtConfig, TimeSpan.FromDays(jwtConfig.ExpireDays), out string refreshJti);
            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);

            var outstandingToken = new OutstandingToken
            {
                Token = refreshTokenString,
                Jti = refreshJti,
                UserId = person.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(jwtConfig.ExpireDays)
            };
            db.OutstandingTokens.Add(outstandingToken);
            await db.SaveChangesAsync();

            return Results.Ok(new 
            { 
                access = new JwtSecurityTokenHandler().WriteToken(accessToken),
                refresh = refreshTokenString
            });
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithOpenApi();

        // Token Refresh Endpoint (POST /api/v1/token/refresh) -> Validates refresh, checks DB, rotates refresh
        authGroup.MapPost("/token/refresh", async (RefreshRequest request, ApplicationDbContext db, JwtConfiguration jwtConfig) =>
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters(jwtConfig);

            SecurityToken validatedToken;
            try
            {
                var principal = handler.ValidateToken(request.Refresh, validationParameters, out validatedToken);
                if (validatedToken is not JwtSecurityToken jwtToken || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Results.BadRequest("Invalid token");
                }

                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti))
                {
                    return Results.BadRequest("Missing JTI");
                }

                // Find outstanding token by JTI
                var outstandingToken = await db.OutstandingTokens.FirstOrDefaultAsync(ot => ot.Jti == jti);
                if (outstandingToken == null || outstandingToken.ExpiresAt < DateTime.UtcNow)
                {
                    return Results.Unauthorized();
                }

                // Check if blacklisted
                if (await db.BlacklistedTokens.AnyAsync(bt => bt.TokenId == outstandingToken.Id))
                {
                    return Results.Unauthorized();
                }

                // Generate new access token
                var newClaims = principal.Claims.ToArray(); // Reuse claims
                var newAccessToken = GenerateToken(newClaims, jwtConfig, TimeSpan.FromMinutes(15), out _);

                // Rotate refresh token: Generate new one, store it, and blacklist the old one
                var newRefreshToken = GenerateToken(newClaims, jwtConfig, TimeSpan.FromDays(jwtConfig.ExpireDays), out string newRefreshJti);
                var newRefreshTokenString = new JwtSecurityTokenHandler().WriteToken(newRefreshToken);

                var newOutstandingToken = new OutstandingToken
                {
                    Token = newRefreshTokenString,
                    Jti = newRefreshJti,
                    UserId = outstandingToken.UserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(jwtConfig.ExpireDays)
                };
                db.OutstandingTokens.Add(newOutstandingToken);

                // Blacklist old refresh token
                db.BlacklistedTokens.Add(new BlacklistedToken
                {
                    TokenId = outstandingToken.Id,
                    BlacklistedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();

                return Results.Ok(new 
                { 
                    access = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                    refresh = newRefreshTokenString
                });
            }
            catch
            {
                // Invalid Refresh Token
                return Results.Unauthorized();
            }
        })
        .AllowAnonymous()
        .WithName("TokenRefresh")
        .WithOpenApi();

        // Token Logout Endpoint (POST /api/v1/token/logout) -> Blacklists the refresh token
        authGroup.MapPost("/token/logout", async (LogoutRequest request, ApplicationDbContext db) =>
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(request.Refresh);
            var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(jti))
            {
                return Results.BadRequest("Missing JTI");
            }

            var outstandingToken = await db.OutstandingTokens.FirstOrDefaultAsync(ot => ot.Jti == jti);
            if (outstandingToken == null)
            {
                return Results.BadRequest("Invalid token");
            }

            // Add to blacklist if not already
            if (!await db.BlacklistedTokens.AnyAsync(bt => bt.TokenId == outstandingToken.Id))
            {
                db.BlacklistedTokens.Add(new BlacklistedToken
                {
                    TokenId = outstandingToken.Id,
                    BlacklistedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            return Results.Ok("Logged out successfully");
        })
        .RequireAuthorization()
        .WithName("TokenLogout")
        .WithOpenApi();
    }

    private static JwtSecurityToken GenerateToken(Claim[] claims, JwtConfiguration jwtConfig, TimeSpan expiry, out string jti)
    {
        jti = Guid.NewGuid().ToString();
        var allClaims = claims.Append(new Claim(JwtRegisteredClaimNames.Jti, jti)).ToArray();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            issuer: jwtConfig.Issuer,
            audience: jwtConfig.Audience,
            claims: allClaims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: creds
        );
    }

    private static TokenValidationParameters GetValidationParameters(JwtConfiguration jwtConfig)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
        };
    }
}

// DTOs for requests (same as before)
public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RefreshRequest
{
    public string Refresh { get; set; }
}

public class LogoutRequest
{
    public string Refresh { get; set; }
}


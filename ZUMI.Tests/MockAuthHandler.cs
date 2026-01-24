/*namespace ZUMI.Tests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

public class MockAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // Eine feste User-ID für unsere Tests
    public const string DefaultUserId = "11111111-1111-1111-1111-111111111111";

    public MockAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Wir erlauben, die UserID per Header zu überschreiben (für Tests mit "fremden" Usern)
        var userId = Request.Headers["X-Test-UserId"].FirstOrDefault() ?? DefaultUserId;

        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "TestUser")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}*/
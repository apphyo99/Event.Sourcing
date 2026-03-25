using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EventSourcing.Integration.Tests.Api;

/// <summary>
/// Test authentication scheme options
/// </summary>
public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = "test-user-id";
    public string DefaultUserName { get; set; } = "Test User";
}

/// <summary>
/// Test authentication handler that bypasses real authentication for testing
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, Options.DefaultUserName),
            new Claim(ClaimTypes.NameIdentifier, Options.DefaultUserId),
            new Claim("sub", Options.DefaultUserId),
            new Claim("name", Options.DefaultUserName)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

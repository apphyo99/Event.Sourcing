using Microsoft.AspNetCore.Authentication;

namespace EventSourcing.Command.Api.Authentication.Testing;

/// <summary>
/// Options for the test authentication scheme (integration / automated tests).
/// </summary>
public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string DefaultUserId { get; set; } = "test-user-id";

    public string DefaultUserName { get; set; } = "Test User";
}

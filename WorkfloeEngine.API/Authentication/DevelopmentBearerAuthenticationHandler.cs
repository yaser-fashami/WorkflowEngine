using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace WorkflowEngine.API.Authentication;

/// <summary>
/// Development-only shared-token authentication. This is not JWT and must never be enabled in production.
/// </summary>
public sealed class DevelopmentBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevelopmentBearer";
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public DevelopmentBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration,
        IHostEnvironment environment)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _environment = environment;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_environment.IsDevelopment())
            return Task.FromResult(AuthenticateResult.Fail("Development bearer authentication is disabled outside Development."));

        var expectedToken = _configuration["Authentication:DevelopmentBearerToken"];
        if (string.IsNullOrWhiteSpace(expectedToken))
            return Task.FromResult(AuthenticateResult.Fail("Development bearer token is not configured."));

        var authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var suppliedToken = authorization[prefix.Length..].Trim();
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedToken);

        if (suppliedBytes.Length == 0 ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
            return Task.FromResult(AuthenticateResult.Fail("Invalid development bearer token."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "development-sinahrm-service"),
            new Claim("service_id", "sinahrm-api"),
            new Claim("scope", "workflow.start workflow.execute")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

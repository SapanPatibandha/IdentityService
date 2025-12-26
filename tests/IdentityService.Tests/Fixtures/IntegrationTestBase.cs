using IdentityService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IdentityService.Tests.Fixtures;

public class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IdentityServiceWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected IdentityDbContext? DbContext;
    private IServiceScope? _serviceScope;

    public IntegrationTestBase()
    {
        Factory = new IdentityServiceWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Create a scope that will remain alive during the test
        _serviceScope = Factory.Services.CreateScope();
        DbContext = _serviceScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _serviceScope?.Dispose();
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Generate a valid JWT token for testing
    /// </summary>
    protected string GenerateAccessToken(Guid userId, Guid clientId, List<string>? scopes = null)
    {
        var secretKey = "IdentityServiceSecretKeyForDevelopmentAndTestingPurposes1234567890!@#$%";
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim("client_id", clientId.ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
        };

        if (scopes != null && scopes.Count > 0)
        {
            claims.Add(new Claim("scope", string.Join(" ", scopes)));
        }

        var token = new JwtSecurityToken(
            issuer: "IdentityService",
            audience: "IdentityService",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Add authorization header to the HTTP client
    /// </summary>
    protected void AddAuthorizationHeader(Guid userId, Guid clientId, List<string>? scopes = null)
    {
        var token = GenerateAccessToken(userId, clientId, scopes);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clear all data from database
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        if (DbContext == null)
            return;

        DbContext.AuditLogs.RemoveRange(DbContext.AuditLogs);
        DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
        DbContext.TwoFactorVerifications.RemoveRange(DbContext.TwoFactorVerifications);
        DbContext.Scopes.RemoveRange(DbContext.Scopes);
        DbContext.Clients.RemoveRange(DbContext.Clients);
        DbContext.Users.RemoveRange(DbContext.Users);

        await DbContext.SaveChangesAsync();
    }
}

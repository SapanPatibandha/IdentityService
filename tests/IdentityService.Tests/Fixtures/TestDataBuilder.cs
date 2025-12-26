using IdentityService.Core.Entities;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Tests.Fixtures;

public class TestDataBuilder
{
    public static User CreateTestUser(string username = "testuser", string email = "test@example.com", string password = "Test@1234")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsEmailVerified = true,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Client CreateTestClient(string name = "Test Client", string clientType = "public")
    {
        var clientId = Guid.NewGuid();
        return new Client
        {
            Id = clientId,
            ClientId = $"client_{clientId}",
            ClientSecret = GenerateSecureString(32),
            Name = name,
            Description = "Test Client Description",
            ClientType = clientType,
            IsActive = true,
            AccessTokenLifetime = 3600,
            RefreshTokenLifetime = 604800,
            CreatedAt = DateTime.UtcNow,
            AllowedScopes = new List<Scope>()
        };
    }

    public static Scope CreateTestScope(string name = "openid", string displayName = "Open ID")
    {
        return new Scope
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = "Test Scope Description",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static RefreshToken CreateTestRefreshToken(User user, Client client)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClientId = client.Id,
            Token = GenerateSecureString(32),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };
    }

    public static AuditLog CreateTestAuditLog(User? user, Client? client)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            ClientId = client?.Id,
            Action = "TEST_ACTION",
            Resource = "TestResource",
            Success = true,
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1"
        };
    }

    private static string GenerateSecureString(int length)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var result = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            result.Append(validChars[random.Next(validChars.Length)]);
        }
        return result.ToString();
    }
}

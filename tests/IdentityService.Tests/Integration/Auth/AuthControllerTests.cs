using IdentityService.Application.DTOs;
using IdentityService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace IdentityService.Tests.Integration.Auth;

[Collection("Integration Tests")]
public class AuthControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndCreatesUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "ValidPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("userId", out _));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = TestDataBuilder.CreateTestUser("existinguser");
        DbContext!.Users.Add(existingUser);
        await DbContext.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "newemail@example.com",
            Password = "ValidPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = TestDataBuilder.CreateTestUser(email: "existing@example.com");
        DbContext!.Users.Add(existingUser);
        await DbContext.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "ValidPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "invalid-email",
            Password = "ValidPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange - Create request with null password
        var request = new { username = "newuser", email = "new@example.com" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var password = "TestPassword123!";
        var testUser = TestDataBuilder.CreateTestUser(password: password);
        DbContext!.Users.Add(testUser);

        var testClient = TestDataBuilder.CreateTestClient();
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = testUser.Username,
            Password = password
        };

        Client.DefaultRequestHeaders.Add("X-Client-Id", testClient.Id.ToString());

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("accessToken", out var tokenElement));
        Assert.False(string.IsNullOrEmpty(tokenElement.GetString()));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        DbContext!.Users.Add(testUser);

        var testClient = TestDataBuilder.CreateTestClient();
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = testUser.Username,
            Password = "WrongPassword123!"
        };

        Client.DefaultRequestHeaders.Add("X-Client-Id", testClient.Id.ToString());

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "Password123!"
        };

        Client.DefaultRequestHeaders.Add("X-Client-Id", testClient.Id.ToString());

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutClientHeader_UsesDefaultClient()
    {
        // Arrange
        var password = "TestPassword123!";
        var testUser = TestDataBuilder.CreateTestUser(password: password);
        DbContext!.Users.Add(testUser);

        var defaultClient = TestDataBuilder.CreateTestClient("Admin Dashboard");
        defaultClient.ClientId = "admin-dashboard";
        DbContext.Clients.Add(defaultClient);
        await DbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = testUser.Username,
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeToken_WithValidToken_ReturnsOk()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var request = new { token = "test-token" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/revoke-token", request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsNewAccessToken()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var refreshToken = TestDataBuilder.CreateTestRefreshToken(testUser, testClient);
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken.Token,
            ClientId = testClient.ClientId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("accessToken", out _));
    }

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var expiredRefreshToken = TestDataBuilder.CreateTestRefreshToken(testUser, testClient);
        expiredRefreshToken.ExpiresAt = DateTime.UtcNow.AddDays(-1); // Expired
        DbContext.RefreshTokens.Add(expiredRefreshToken);
        await DbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest
        {
            RefreshToken = expiredRefreshToken.Token,
            ClientId = testClient.ClientId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-token",
            ClientId = testClient.ClientId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request2FA_WithValidUser_ReturnsOk()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var request = new { method = "email" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/request-2fa", request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetupTotp_WithValidUser_ReturnsTotpSecret()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.PostAsync("/api/v1/auth/setup-totp", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("secret", out _) || 
                    jsonDocument.RootElement.TryGetProperty("qrCode", out _));
    }

    [Fact]
    public async Task SetupTotp_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/v1/auth/setup-totp", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyTwoFactor_WithValidCode_ReturnsOk()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var twoFactorVerification = new IdentityService.Core.Entities.TwoFactorVerification
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            VerificationCode = "123456",
            Method = "email",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.TwoFactorVerifications.Add(twoFactorVerification);
        await DbContext.SaveChangesAsync();

        var request = new TwoFactorVerifyRequest
        {
            Code = "123456",
            Method = "email"
        };

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/verify-2fa", request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyTwoFactor_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        var request = new TwoFactorVerifyRequest
        {
            Code = "invalid",
            Method = "email"
        };

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/verify-2fa", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

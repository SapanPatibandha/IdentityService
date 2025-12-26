using IdentityService.Application.DTOs;
using IdentityService.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace IdentityService.Tests.Integration.Admin;

[Collection("Integration Tests")]
public class AdminControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateClient_WithValidData_ReturnsCreatedAndClientId()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var request = new CreateClientRequest
        {
            Name = "New Test Client",
            Description = "A new test client for testing",
            ClientType = "public",
            AllowedScopeNames = new List<string> { "openid", "profile" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/admin/clients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("clientId", out var clientId));
        Assert.False(string.IsNullOrEmpty(clientId.GetString()));
    }

    [Fact]
    public async Task CreateClient_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateClientRequest
        {
            Name = "New Test Client",
            Description = "A new test client for testing",
            ClientType = "public"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/admin/clients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateClient_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var request = new { description = "Test", clientType = "public" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/admin/clients", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllClients_WithValidToken_ReturnsClientList()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient("Admin Client");
        var otherClient = TestDataBuilder.CreateTestClient("Other Client");
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        DbContext.Clients.Add(otherClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync("/api/v1/admin/clients");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllClients_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/admin/clients");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClientById_WithValidId_ReturnsClient()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient("Test Client");
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/clients/{testClient.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("id", out var id));
        Assert.Equal(testClient.Id.ToString(), id.GetString());
    }

    [Fact]
    public async Task GetClientById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/clients/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetClientById_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/clients/{testClient.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateScope_WithValidData_ReturnsCreatedAndScopeId()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var request = new CreateScopeRequest
        {
            Name = "email",
            DisplayName = "Email",
            Description = "Access to user email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/admin/scopes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreateScope_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateScopeRequest
        {
            Name = "email",
            DisplayName = "Email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/admin/scopes", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateScope_WithMissingName_ReturnsBadRequest()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var request = new { displayName = "Email" };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/admin/scopes", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllScopes_WithValidToken_ReturnsScopeList()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        var scope = TestDataBuilder.CreateTestScope();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        DbContext.Scopes.Add(scope);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync("/api/v1/admin/scopes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllScopes_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/admin/scopes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetScopeById_WithValidId_ReturnsScope()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        var scope = TestDataBuilder.CreateTestScope();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        DbContext.Scopes.Add(scope);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/scopes/{scope.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("id", out var id));
        Assert.Equal(scope.Id.ToString(), id.GetString());
    }

    [Fact]
    public async Task GetScopeById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/scopes/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_WithValidToken_ReturnsUserList()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var anotherUser = TestDataBuilder.CreateTestUser("anotheruser");
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Users.Add(anotherUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync("/api/v1/admin/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllUsers_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/admin/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var targetUser = TestDataBuilder.CreateTestUser("targetuser");
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Users.Add(targetUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/users/{targetUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.TryGetProperty("id", out var id));
        Assert.Equal(targetUser.Id.ToString(), id.GetString());
    }

    [Fact]
    public async Task GetUserById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/users/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        DbContext!.Users.Add(testUser);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/v1/admin/users/{testUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithValidToken_ReturnsAuditLogList()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        var auditLog = TestDataBuilder.CreateTestAuditLog(testUser, testClient);
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);
        DbContext.AuditLogs.Add(auditLog);
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync("/api/v1/admin/audit-logs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(content);
        Assert.True(jsonDocument.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutAuthorization_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/admin/audit-logs");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithPagination_ReturnsPaginatedList()
    {
        // Arrange
        var testUser = TestDataBuilder.CreateTestUser();
        var testClient = TestDataBuilder.CreateTestClient();
        DbContext!.Users.Add(testUser);
        DbContext.Clients.Add(testClient);

        // Add multiple audit logs
        for (int i = 0; i < 15; i++)
        {
            var auditLog = new IdentityService.Core.Entities.AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = testUser.Id,
                ClientId = testClient.Id,
                Action = $"ACTION_{i}",
                Resource = "TestResource",
                Success = true,
                CreatedAt = DateTime.UtcNow,
                IpAddress = "127.0.0.1"
            };
            DbContext.AuditLogs.Add(auditLog);
        }
        await DbContext.SaveChangesAsync();

        AddAuthorizationHeader(testUser.Id, testClient.Id);

        // Act
        var response = await Client.GetAsync("/api/v1/admin/audit-logs?skip=0&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

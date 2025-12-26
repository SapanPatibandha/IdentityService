using Moq;
using Xunit;
using IdentityService.Core.Interfaces;
using IdentityService.Application.Services;
using IdentityService.Core.Entities;
using IdentityService.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace IdentityService.Tests.Unit.Services;

public class TokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("IdentityServiceSecretKeyForDevelopmentAndTestingPurposes1234567890!@#$%"));
        _tokenService = new TokenService(_mockRefreshTokenRepository.Object, _mockUserRepository.Object, _mockConfiguration.Object, signingKey);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var user = TestDataBuilder.CreateTestUser();
        var scopes = new List<string> { "openid", "profile" };
        var clientId = Guid.NewGuid();

        // Act
        var token = _tokenService.GenerateAccessToken(user, scopes, clientId);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        Assert.True(token.Split('.').Length == 3); // JWT format: header.payload.signature
    }

    [Fact]
    public void GenerateAccessToken_WithEmptyScopes_ReturnsValidToken()
    {
        // Arrange
        var user = TestDataBuilder.CreateTestUser();
        var scopes = new List<string>();
        var clientId = Guid.NewGuid();

        // Act
        var token = _tokenService.GenerateAccessToken(user, scopes, clientId);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithValidData_ReturnsRefreshToken()
    {
        // Arrange
        var user = TestDataBuilder.CreateTestUser();
        var clientId = Guid.NewGuid();
        var ipAddress = "127.0.0.1";
        var userAgent = "Test Agent";

        _mockRefreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user, clientId, ipAddress, userAgent);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.Equal(clientId, refreshToken.ClientId);
        Assert.Equal(user.Id, refreshToken.UserId);
        Assert.Equal(ipAddress, refreshToken.IpAddress);
        Assert.Equal(userAgent, refreshToken.UserAgent);
        _mockRefreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
    }
}

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITwoFactorVerificationRepository> _mockTwoFactorRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTwoFactorRepository = new Mock<ITwoFactorVerificationRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _authenticationService = new AuthenticationService(
            _mockUserRepository.Object,
            _mockTwoFactorRepository.Object,
            _mockEmailService.Object,
            _mockAuditLogService.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesNewUser()
    {
        // Arrange
        var username = "newuser";
        var email = "newuser@example.com";
        var password = "ValidPassword123!";

        _mockUserRepository
            .Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var (success, user, errorMessage) = await _authenticationService.RegisterAsync(
            username, email, password, "John", "Doe");

        // Assert
        Assert.True(success);
        Assert.NotNull(user);
        Assert.Equal(username, user.Username);
        Assert.Equal(email, user.Email);
        Assert.Null(errorMessage);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ReturnsFalse()
    {
        // Arrange
        var existingUser = TestDataBuilder.CreateTestUser();
        var username = existingUser.Username;

        _mockUserRepository
            .Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync(existingUser);

        // Act
        var (success, user, errorMessage) = await _authenticationService.RegisterAsync(
            username, "newemail@example.com", "Password123!", "John", "Doe");

        // Assert
        Assert.False(success);
        Assert.Null(user);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsFalse()
    {
        // Arrange
        var existingUser = TestDataBuilder.CreateTestUser();

        _mockUserRepository
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(existingUser.Email))
            .ReturnsAsync(existingUser);

        // Act
        var (success, user, errorMessage) = await _authenticationService.RegisterAsync(
            "newuser", existingUser.Email, "Password123!", "John", "Doe");

        // Assert
        Assert.False(success);
        Assert.Null(user);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var password = "TestPassword123!";
        var user = TestDataBuilder.CreateTestUser(password: password);
        user.PasswordHash = await _authenticationService.HashPasswordAsync(password);

        _mockUserRepository
            .Setup(r => r.GetByUsernameAsync(user.Username))
            .ReturnsAsync(user);

        // Act
        var (success, returnedUser, errorMessage) = await _authenticationService.LoginAsync(
            user.Username, password);

        // Assert
        Assert.True(success);
        Assert.NotNull(returnedUser);
        Assert.Equal(user.Id, returnedUser.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFalse()
    {
        // Arrange
        var user = TestDataBuilder.CreateTestUser();

        _mockUserRepository
            .Setup(r => r.GetByUsernameAsync(user.Username))
            .ReturnsAsync(user);

        // Act
        var (success, returnedUser, errorMessage) = await _authenticationService.LoginAsync(
            user.Username, "WrongPassword123!");

        // Assert
        Assert.False(success);
        Assert.Null(returnedUser);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var (success, returnedUser, errorMessage) = await _authenticationService.LoginAsync(
            "nonexistent", "Password123!");

        // Assert
        Assert.False(success);
        Assert.Null(returnedUser);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = await _authenticationService.HashPasswordAsync(password);

        // Act
        var isValid = await _authenticationService.ValidatePasswordAsync(password, hashedPassword);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithInvalidPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = await _authenticationService.HashPasswordAsync(password);

        // Act
        var isValid = await _authenticationService.ValidatePasswordAsync("WrongPassword", hashedPassword);

        // Assert
        Assert.False(isValid);
    }
}

public class ClientServiceTests
{
    private readonly Mock<IClientRepository> _mockClientRepository;
    private readonly Mock<IScopeRepository> _mockScopeRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly ClientService _clientService;

    public ClientServiceTests()
    {
        _mockClientRepository = new Mock<IClientRepository>();
        _mockScopeRepository = new Mock<IScopeRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _clientService = new ClientService(_mockClientRepository.Object, _mockScopeRepository.Object, _mockAuditLogService.Object);
    }

    [Fact]
    public async Task CreateClientAsync_WithValidData_CreatesNewClient()
    {
        // Arrange
        var name = "Test Client";
        var description = "Test Description";
        var clientType = "public";
        var scopeNames = new List<string> { "openid" };

        _mockClientRepository
            .Setup(r => r.AddAsync(It.IsAny<Client>()))
            .Returns(Task.CompletedTask);

        _mockScopeRepository
            .Setup(r => r.GetByNamesAsync(scopeNames))
            .ReturnsAsync(new List<Scope> { TestDataBuilder.CreateTestScope() });

        // Act
        var client = await _clientService.CreateClientAsync(name, description, clientType, scopeNames);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(name, client.Name);
        Assert.Equal(description, client.Description);
        Assert.Equal(clientType, client.ClientType);
    }

    [Fact]
    public async Task GetClientAsync_WithValidId_ReturnsClient()
    {
        // Arrange
        var client = TestDataBuilder.CreateTestClient();

        _mockClientRepository
            .Setup(r => r.GetByIdAsync(client.Id))
            .ReturnsAsync(client);

        // Act
        var result = await _clientService.GetClientAsync(client.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.Id, result.Id);
    }

    [Fact]
    public async Task GetClientAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        _mockClientRepository
            .Setup(r => r.GetByIdAsync(invalidId))
            .ReturnsAsync((Client?)null);

        // Act
        var result = await _clientService.GetClientAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllClientsAsync_ReturnsAllClients()
    {
        // Arrange
        var clients = new List<Client>
        {
            TestDataBuilder.CreateTestClient("Client 1"),
            TestDataBuilder.CreateTestClient("Client 2")
        };

        _mockClientRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(clients);

        // Act
        var result = await _clientService.GetAllClientsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ValidateClientAsync_WithValidCredentials_ReturnsClient()
    {
        // Arrange
        var client = TestDataBuilder.CreateTestClient();

        _mockClientRepository
            .Setup(r => r.GetByClientIdAsync(client.ClientId))
            .ReturnsAsync(client);

        // Act
        var result = await _clientService.ValidateClientAsync(client.ClientId, client.ClientSecret);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(client.Id, result.Id);
    }

    [Fact]
    public async Task ValidateClientAsync_WithInvalidSecret_ReturnsNull()
    {
        // Arrange
        var client = TestDataBuilder.CreateTestClient();

        _mockClientRepository
            .Setup(r => r.GetByClientIdAsync(client.ClientId))
            .ReturnsAsync(client);

        // Act
        var result = await _clientService.ValidateClientAsync(client.ClientId, "invalid-secret");

        // Assert
        Assert.Null(result);
    }
}

public class ScopeServiceTests
{
    private readonly Mock<IScopeRepository> _mockScopeRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly ScopeService _scopeService;

    public ScopeServiceTests()
    {
        _mockScopeRepository = new Mock<IScopeRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _scopeService = new ScopeService(_mockScopeRepository.Object, _mockAuditLogService.Object);
    }

    [Fact]
    public async Task CreateScopeAsync_WithValidData_CreatesNewScope()
    {
        // Arrange
        var name = "email";
        var displayName = "Email";
        var description = "Access to email";

        _mockScopeRepository
            .Setup(r => r.AddAsync(It.IsAny<Scope>()))
            .Returns(Task.CompletedTask);

        // Act
        var scope = await _scopeService.CreateScopeAsync(name, displayName, description);

        // Assert
        Assert.NotNull(scope);
        Assert.Equal(name, scope.Name);
        Assert.Equal(displayName, scope.DisplayName);
    }

    [Fact]
    public async Task GetScopeAsync_WithValidId_ReturnsScope()
    {
        // Arrange
        var scope = TestDataBuilder.CreateTestScope();

        _mockScopeRepository
            .Setup(r => r.GetByIdAsync(scope.Id))
            .ReturnsAsync(scope);

        // Act
        var result = await _scopeService.GetScopeAsync(scope.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scope.Id, result.Id);
    }

    [Fact]
    public async Task GetAllScopesAsync_ReturnsAllScopes()
    {
        // Arrange
        var scopes = new List<Scope>
        {
            TestDataBuilder.CreateTestScope("openid", "Open ID"),
            TestDataBuilder.CreateTestScope("profile", "Profile")
        };

        _mockScopeRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(scopes);

        // Act
        var result = await _scopeService.GetAllScopesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}

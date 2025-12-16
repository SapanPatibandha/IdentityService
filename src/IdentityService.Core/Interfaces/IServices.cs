using IdentityService.Core.Entities;

namespace IdentityService.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, List<string> scopes, Guid clientId);
    Task<RefreshToken> GenerateRefreshTokenAsync(User user, Guid clientId, string ipAddress, string userAgent);
    Task<(string AccessToken, RefreshToken? NewRefreshToken)> RefreshAccessTokenAsync(RefreshToken currentRefreshToken, string ipAddress);
    Task RevokeTokenAsync(string token);
    (Guid UserId, List<string> Scopes, Guid ClientId) ValidateToken(string token);
}

public interface IAuthenticationService
{
    Task<(bool Success, User? User, string? ErrorMessage)> RegisterAsync(string username, string email, string password, string? firstName, string? lastName);
    Task<(bool Success, User? User, string? ErrorMessage)> LoginAsync(string username, string password);
    Task<bool> VerifyEmailAsync(string token);
    Task<(bool Success, string? Token)> InitiateTwoFactorAsync(User user, string method);
    Task<(bool Success, string? Message)> VerifyTwoFactorAsync(User user, string code, string method);
    Task<(string Secret, string QrCode)> SetupTotpAsync(User user, string appName = "IdentityService");
    Task<bool> ValidatePasswordAsync(string password, string passwordHash);
    Task<string> HashPasswordAsync(string password);
}

public interface IClientService
{
    Task<Client> CreateClientAsync(string name, string? description, string clientType, List<string>? allowedScopeNames);
    Task<Client?> GetClientAsync(Guid clientId);
    Task<Client?> ValidateClientAsync(string clientId, string clientSecret);
    Task<List<Scope>> GetClientScopesAsync(Guid clientId);
    Task UpdateClientAsync(Client client);
    Task DeleteClientAsync(Guid clientId);
}

public interface IScopeService
{
    Task<Scope> CreateScopeAsync(string name, string displayName, string? description);
    Task<Scope?> GetScopeAsync(Guid scopeId);
    Task<Scope?> GetScopeByNameAsync(string name);
    Task<List<Scope>> GetAllScopesAsync();
    Task<List<Scope>> GetScopesByNamesAsync(List<string> names);
    Task UpdateScopeAsync(Scope scope);
    Task DeleteScopeAsync(Guid scopeId);
}

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, Guid? clientId, string action, string resource, bool success, string? description = null, string? errorMessage = null);
    Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int skip = 0, int take = 100);
    Task<List<AuditLog>> GetClientAuditLogsAsync(Guid clientId, int skip = 0, int take = 100);
    Task<List<AuditLog>> GetAllAuditLogsAsync(int skip = 0, int take = 100);
}

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string token);
    Task SendTwoFactorCodeAsync(string email, string code);
    Task SendPasswordResetAsync(string email, string token);
}

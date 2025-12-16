using IdentityService.Core.Entities;

namespace IdentityService.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetAllAsync(int skip = 0, int take = 100);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string username, string email);
}

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByClientIdAsync(string clientId);
    Task<List<Client>> GetAllAsync(int skip = 0, int take = 100);
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
}

public interface IScopeRepository
{
    Task<Scope?> GetByIdAsync(Guid id);
    Task<Scope?> GetByNameAsync(string name);
    Task<List<Scope>> GetByNamesAsync(List<string> names);
    Task<List<Scope>> GetAllAsync();
    Task AddAsync(Scope scope);
    Task UpdateAsync(Scope scope);
    Task DeleteAsync(Guid id);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<List<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task DeleteAsync(Guid id);
    Task RevokeAsync(Guid id, string reason);
    Task RevokeUserTokensAsync(Guid userId);
}

public interface ITwoFactorVerificationRepository
{
    Task<TwoFactorVerification?> GetByIdAsync(Guid id);
    Task<TwoFactorVerification?> GetPendingByUserAndMethodAsync(Guid userId, string method);
    Task AddAsync(TwoFactorVerification verification);
    Task UpdateAsync(TwoFactorVerification verification);
    Task DeleteAsync(Guid id);
}

public interface IAuditLogRepository
{
    Task<AuditLog?> GetByIdAsync(Guid id);
    Task<List<AuditLog>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 100);
    Task<List<AuditLog>> GetByClientIdAsync(Guid clientId, int skip = 0, int take = 100);
    Task<List<AuditLog>> GetAllAsync(int skip = 0, int take = 100);
    Task AddAsync(AuditLog log);
}

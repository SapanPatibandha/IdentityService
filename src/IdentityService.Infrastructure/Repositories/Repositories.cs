using Microsoft.EntityFrameworkCore;
using IdentityService.Core.Entities;
using IdentityService.Core.Interfaces;
using IdentityService.Infrastructure.Data;

namespace IdentityService.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IdentityDbContext context) : base(context) { }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<List<User>> GetAllAsync(int skip = 0, int take = 100)
    {
        return await _dbSet.AsNoTracking().Skip(skip).Take(take).ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _dbSet.AddAsync(user);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _dbSet.Update(user);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == id);
        if (user != null)
        {
            _dbSet.Remove(user);
            await SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        return await _dbSet.AnyAsync(u => u.Username == username || u.Email == email);
    }
}

public class ClientRepository : BaseRepository<Client>, IClientRepository
{
    public ClientRepository(IdentityDbContext context) : base(context) { }

    public async Task<Client?> GetByIdAsync(Guid id)
    {
        return await _dbSet.Include(c => c.AllowedScopes).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Client?> GetByClientIdAsync(string clientId)
    {
        return await _dbSet.Include(c => c.AllowedScopes).AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<List<Client>> GetAllAsync(int skip = 0, int take = 100)
    {
        return await _dbSet.Include(c => c.AllowedScopes).AsNoTracking().Skip(skip).Take(take).ToListAsync();
    }

    public async Task AddAsync(Client client)
    {
        await _dbSet.AddAsync(client);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(Client client)
    {
        _dbSet.Update(client);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await _dbSet.FirstOrDefaultAsync(c => c.Id == id);
        if (client != null)
        {
            _dbSet.Remove(client);
            await SaveChangesAsync();
        }
    }
}

public class ScopeRepository : BaseRepository<Scope>, IScopeRepository
{
    public ScopeRepository(IdentityDbContext context) : base(context) { }

    public async Task<Scope?> GetByIdAsync(Guid id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Scope?> GetByNameAsync(string name)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<List<Scope>> GetByNamesAsync(List<string> names)
    {
        return await _dbSet.AsNoTracking().Where(s => names.Contains(s.Name)).ToListAsync();
    }

    public async Task<List<Scope>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(Scope scope)
    {
        await _dbSet.AddAsync(scope);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(Scope scope)
    {
        _dbSet.Update(scope);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var scope = await _dbSet.FirstOrDefaultAsync(s => s.Id == id);
        if (scope != null)
        {
            _dbSet.Remove(scope);
            await SaveChangesAsync();
        }
    }
}

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(rt => rt.Token == token && rt.RevokedAt == null);
    }

    public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet.AsNoTracking().Where(rt => rt.UserId == userId && rt.RevokedAt == null).ToListAsync();
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _dbSet.AddAsync(token);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken token)
    {
        _dbSet.Update(token);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var token = await _dbSet.FirstOrDefaultAsync(rt => rt.Id == id);
        if (token != null)
        {
            _dbSet.Remove(token);
            await SaveChangesAsync();
        }
    }

    public async Task RevokeAsync(Guid id, string reason)
    {
        var token = await _dbSet.FirstOrDefaultAsync(rt => rt.Id == id);
        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokeReason = reason;
            await SaveChangesAsync();
        }
    }

    public async Task RevokeUserTokensAsync(Guid userId)
    {
        var tokens = await _dbSet.Where(rt => rt.UserId == userId && rt.RevokedAt == null).ToListAsync();
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokeReason = "User logout";
        }
        await SaveChangesAsync();
    }
}

public class TwoFactorVerificationRepository : BaseRepository<TwoFactorVerification>, ITwoFactorVerificationRepository
{
    public TwoFactorVerificationRepository(IdentityDbContext context) : base(context) { }

    public async Task<TwoFactorVerification?> GetByIdAsync(Guid id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(tf => tf.Id == id);
    }

    public async Task<TwoFactorVerification?> GetPendingByUserAndMethodAsync(Guid userId, string method)
    {
        return await _dbSet.AsNoTracking()
            .Where(tf => tf.UserId == userId && tf.Method == method && !tf.IsVerified && tf.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(tf => tf.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(TwoFactorVerification verification)
    {
        await _dbSet.AddAsync(verification);
        await SaveChangesAsync();
    }

    public async Task UpdateAsync(TwoFactorVerification verification)
    {
        _dbSet.Update(verification);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var verification = await _dbSet.FirstOrDefaultAsync(tf => tf.Id == id);
        if (verification != null)
        {
            _dbSet.Remove(verification);
            await SaveChangesAsync();
        }
    }
}

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(IdentityDbContext context) : base(context) { }

    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(al => al.Id == id);
    }

    public async Task<List<AuditLog>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 100)
    {
        return await _dbSet.AsNoTracking().Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<List<AuditLog>> GetByClientIdAsync(Guid clientId, int skip = 0, int take = 100)
    {
        return await _dbSet.AsNoTracking().Where(al => al.ClientId == clientId)
            .OrderByDescending(al => al.CreatedAt).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<List<AuditLog>> GetAllAsync(int skip = 0, int take = 100)
    {
        return await _dbSet.AsNoTracking().OrderByDescending(al => al.CreatedAt).Skip(skip).Take(take).ToListAsync();
    }

    public async Task AddAsync(AuditLog log)
    {
        await _dbSet.AddAsync(log);
        await SaveChangesAsync();
    }
}

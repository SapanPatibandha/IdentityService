using IdentityService.Core.Entities;
using IdentityService.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IdentityService.Application.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly IScopeRepository _scopeRepository;
    private readonly IAuditLogService _auditLogService;

    public ClientService(
        IClientRepository clientRepository,
        IScopeRepository scopeRepository,
        IAuditLogService auditLogService)
    {
        _clientRepository = clientRepository;
        _scopeRepository = scopeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Client> CreateClientAsync(string name, string? description, string clientType, List<string>? allowedScopeNames)
    {
        var clientId = Guid.NewGuid().ToString().Substring(0, 16);
        var clientSecret = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();

        var scopes = allowedScopeNames != null 
            ? await _scopeRepository.GetByNamesAsync(allowedScopeNames)
            : new List<Scope>();

        var client = new Client
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecret = BCrypt.Net.BCrypt.HashPassword(clientSecret),
            Name = name,
            Description = description,
            ClientType = clientType,
            IsActive = true,
            AllowedScopes = scopes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };

        await _clientRepository.AddAsync(client);
        return client;
    }

    public async Task<Client?> GetClientAsync(Guid clientId)
    {
        return await _clientRepository.GetByIdAsync(clientId);
    }

    public async Task<Client?> ValidateClientAsync(string clientId, string clientSecret)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId);
        if (client == null || !client.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(clientSecret, client.ClientSecret))
            return null;

        return client;
    }

    public async Task<List<Scope>> GetClientScopesAsync(Guid clientId)
    {
        var client = await _clientRepository.GetByIdAsync(clientId);
        return client?.AllowedScopes.ToList() ?? new List<Scope>();
    }

    public async Task UpdateClientAsync(Client client)
    {
        client.UpdatedAt = DateTime.UtcNow;
        await _clientRepository.UpdateAsync(client);
    }

    public async Task DeleteClientAsync(Guid clientId)
    {
        await _clientRepository.DeleteAsync(clientId);
    }
}

public class ScopeService : IScopeService
{
    private readonly IScopeRepository _scopeRepository;
    private readonly IAuditLogService _auditLogService;

    public ScopeService(
        IScopeRepository scopeRepository,
        IAuditLogService auditLogService)
    {
        _scopeRepository = scopeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Scope> CreateScopeAsync(string name, string displayName, string? description)
    {
        var scope = new Scope
        {
            Id = Guid.NewGuid(),
            Name = name,
            DisplayName = displayName,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _scopeRepository.AddAsync(scope);
        return scope;
    }

    public async Task<Scope?> GetScopeAsync(Guid scopeId)
    {
        return await _scopeRepository.GetByIdAsync(scopeId);
    }

    public async Task<Scope?> GetScopeByNameAsync(string name)
    {
        return await _scopeRepository.GetByNameAsync(name);
    }

    public async Task<List<Scope>> GetAllScopesAsync()
    {
        return await _scopeRepository.GetAllAsync();
    }

    public async Task<List<Scope>> GetScopesByNamesAsync(List<string> names)
    {
        return await _scopeRepository.GetByNamesAsync(names);
    }

    public async Task UpdateScopeAsync(Scope scope)
    {
        scope.UpdatedAt = DateTime.UtcNow;
        await _scopeRepository.UpdateAsync(scope);
    }

    public async Task DeleteScopeAsync(Guid scopeId)
    {
        await _scopeRepository.DeleteAsync(scopeId);
    }
}

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(Guid? userId, Guid? clientId, string action, string resource, bool success, string? description = null, string? errorMessage = null)
    {
        var context = _httpContextAccessor?.HttpContext;
        var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context?.Request.Headers["User-Agent"].ToString() ?? "unknown";

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClientId = clientId,
            Action = action,
            Resource = resource,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int skip = 0, int take = 100)
    {
        return await _auditLogRepository.GetByUserIdAsync(userId, skip, take);
    }

    public async Task<List<AuditLog>> GetClientAuditLogsAsync(Guid clientId, int skip = 0, int take = 100)
    {
        return await _auditLogRepository.GetByClientIdAsync(clientId, skip, take);
    }

    public async Task<List<AuditLog>> GetAllAuditLogsAsync(int skip = 0, int take = 100)
    {
        return await _auditLogRepository.GetAllAsync(skip, take);
    }
}

public class MockEmailService : IEmailService
{
    public async Task SendEmailVerificationAsync(string email, string token)
    {
        // Mock implementation - in production, integrate with SendGrid, AWS SES, etc.
        await Task.CompletedTask;
    }

    public async Task SendTwoFactorCodeAsync(string email, string code)
    {
        // Mock implementation
        await Task.CompletedTask;
    }

    public async Task SendPasswordResetAsync(string email, string token)
    {
        // Mock implementation
        await Task.CompletedTask;
    }
}

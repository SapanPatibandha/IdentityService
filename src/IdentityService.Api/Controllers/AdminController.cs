using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdentityService.Application.DTOs;
using IdentityService.Core.Interfaces;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IScopeService _scopeService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserRepository _userRepository;

    public AdminController(
        IClientService clientService,
        IScopeService scopeService,
        IAuditLogService auditLogService,
        IUserRepository userRepository)
    {
        _clientService = clientService;
        _scopeService = scopeService;
        _auditLogService = auditLogService;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Create a new OAuth2 client
    /// </summary>
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        try
        {
            var client = await _clientService.CreateClientAsync(
                request.Name, request.Description, request.ClientType, request.AllowedScopeNames);

            var clientDto = new ClientSecretDto
            {
                Id = client.Id,
                ClientId = client.ClientId,
                ClientSecret = client.ClientSecret,
                Name = client.Name
            };

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, clientDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get client by ID
    /// </summary>
    [HttpGet("clients/{id}")]
    public async Task<IActionResult> GetClient(Guid id)
    {
        var client = await _clientService.GetClientAsync(id);
        if (client == null)
            return NotFound();

        var dto = new ClientDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            Name = client.Name,
            Description = client.Description,
            ClientType = client.ClientType,
            IsActive = client.IsActive,
            AccessTokenLifetime = client.AccessTokenLifetime,
            RefreshTokenLifetime = client.RefreshTokenLifetime,
            AllowedScopes = client.AllowedScopes.Select(s => s.Name).ToList(),
            CreatedAt = client.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Create a scope
    /// </summary>
    [HttpPost("scopes")]
    public async Task<IActionResult> CreateScope([FromBody] CreateScopeRequest request)
    {
        var scope = await _scopeService.CreateScopeAsync(request.Name, request.DisplayName, request.Description);

        var dto = new ScopeDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            IsActive = scope.IsActive,
            CreatedAt = scope.CreatedAt
        };

        return CreatedAtAction(nameof(GetScope), new { id = scope.Id }, dto);
    }

    /// <summary>
    /// Get scope by ID
    /// </summary>
    [HttpGet("scopes/{id}")]
    public async Task<IActionResult> GetScope(Guid id)
    {
        var scope = await _scopeService.GetScopeAsync(id);
        if (scope == null)
            return NotFound();

        var dto = new ScopeDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            IsActive = scope.IsActive,
            CreatedAt = scope.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get all scopes
    /// </summary>
    [HttpGet("scopes")]
    public async Task<IActionResult> GetAllScopes()
    {
        var scopes = await _scopeService.GetAllScopesAsync();

        var dtos = scopes.Select(s => new ScopeDto
        {
            Id = s.Id,
            Name = s.Name,
            DisplayName = s.DisplayName,
            Description = s.Description,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Get audit logs
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(int skip = 0, int take = 100)
    {
        var logs = await _auditLogService.GetAllAuditLogsAsync(skip, take);

        var dtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            ClientId = l.ClientId,
            Action = l.Action,
            Resource = l.Resource,
            Description = l.Description,
            IpAddress = l.IpAddress,
            Success = l.Success,
            ErrorMessage = l.ErrorMessage,
            CreatedAt = l.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Get user audit logs
    /// </summary>
    [HttpGet("audit-logs/user/{userId}")]
    public async Task<IActionResult> GetUserAuditLogs(Guid userId, int skip = 0, int take = 100)
    {
        var logs = await _auditLogService.GetUserAuditLogsAsync(userId, skip, take);

        var dtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            ClientId = l.ClientId,
            Action = l.Action,
            Resource = l.Resource,
            Description = l.Description,
            IpAddress = l.IpAddress,
            Success = l.Success,
            ErrorMessage = l.ErrorMessage,
            CreatedAt = l.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(int skip = 0, int take = 100)
    {
        var users = await _userRepository.GetAllAsync(skip, take);

        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsEmailVerified = u.IsEmailVerified,
            TwoFactorEnabled = u.TwoFactorEnabled,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Ok(dtos);
    }
}

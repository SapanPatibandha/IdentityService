using Microsoft.AspNetCore.Mvc;
using IdentityService.Application.DTOs;
using IdentityService.Core.Interfaces;

namespace IdentityService.Api.Extensions;

/// <summary>
/// Extension methods for registering Admin endpoints
/// </summary>
public static class AdminEndpointsExtensions
{
    public static void MapAdminEndpoints(this WebApplication app, RouteGroupBuilder apiGroup)
    {
        var adminGroup = apiGroup.MapGroup("/admin")
            .RequireAuthorization();

        // Client endpoints
        adminGroup.MapPost("/clients", CreateClientHandler)
            .WithName("CreateClient")
            .WithSummary("Create a new OAuth2 client");

        adminGroup.MapGet("/clients", GetAllClientsHandler)
            .WithName("GetAllClients")
            .WithSummary("Get all OAuth2 clients");

        adminGroup.MapGet("/clients/{id}", GetClientHandler)
            .WithName("GetClient")
            .WithSummary("Get client by ID");

        // Scope endpoints
        adminGroup.MapPost("/scopes", CreateScopeHandler)
            .WithName("CreateScope")
            .WithSummary("Create a scope");

        adminGroup.MapGet("/scopes/{id}", GetScopeHandler)
            .WithName("GetScope")
            .WithSummary("Get scope by ID");

        adminGroup.MapGet("/scopes", GetAllScopesHandler)
            .WithName("GetAllScopes")
            .WithSummary("Get all scopes");

        // Audit log endpoints
        adminGroup.MapGet("/audit-logs", GetAuditLogsHandler)
            .WithName("GetAuditLogs")
            .WithSummary("Get audit logs");

        adminGroup.MapGet("/audit-logs/user/{userId}", GetUserAuditLogsHandler)
            .WithName("GetUserAuditLogs")
            .WithSummary("Get user audit logs");

        // User endpoints
        adminGroup.MapGet("/users", GetAllUsersHandler)
            .WithName("GetAllUsers")
            .WithSummary("Get all users");
    }

    // ========== Client Handlers ==========

    private static async Task CreateClientHandler(
        CreateClientRequest request,
        IClientService clientService,
        HttpContext context)
    {
        var client = await clientService.CreateClientAsync(
            request.Name, request.Description, request.ClientType, request.AllowedScopeNames);

        var clientDto = new ClientSecretDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientSecret = client.ClientSecret,
            Name = client.Name
        };

        context.Response.StatusCode = 201;
        context.Response.Headers.Location = $"/api/v1/admin/clients/{client.Id}";
        await context.Response.WriteAsJsonAsync(clientDto);
    }

    private static async Task GetAllClientsHandler(IClientService clientService, HttpContext context)
    {
        var clients = await clientService.GetAllClientsAsync();

        var dtos = clients.Select(c => new ClientDto
        {
            Id = c.Id,
            ClientId = c.ClientId,
            Name = c.Name,
            Description = c.Description,
            ClientType = c.ClientType,
            IsActive = c.IsActive,
            AccessTokenLifetime = c.AccessTokenLifetime,
            RefreshTokenLifetime = c.RefreshTokenLifetime,
            AllowedScopes = c.AllowedScopes.Select(s => s.Name).ToList(),
            CreatedAt = c.CreatedAt
        }).ToList();

        await context.Response.WriteAsJsonAsync(dtos);
    }

    private static async Task GetClientHandler(Guid id, IClientService clientService, HttpContext context)
    {
        var client = await clientService.GetClientAsync(id);
        if (client == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { message = "Client not found" });
            return;
        }

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

        await context.Response.WriteAsJsonAsync(dto);
    }

    // ========== Scope Handlers ==========

    private static async Task CreateScopeHandler(
        CreateScopeRequest request,
        IScopeService scopeService,
        HttpContext context)
    {
        var scope = await scopeService.CreateScopeAsync(request.Name, request.DisplayName, request.Description);

        var dto = new ScopeDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            IsActive = scope.IsActive,
            CreatedAt = scope.CreatedAt
        };

        context.Response.StatusCode = 201;
        context.Response.Headers.Location = $"/api/v1/admin/scopes/{scope.Id}";
        await context.Response.WriteAsJsonAsync(dto);
    }

    private static async Task GetScopeHandler(Guid id, IScopeService scopeService, HttpContext context)
    {
        var scope = await scopeService.GetScopeAsync(id);
        if (scope == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { message = "Scope not found" });
            return;
        }

        var dto = new ScopeDto
        {
            Id = scope.Id,
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description,
            IsActive = scope.IsActive,
            CreatedAt = scope.CreatedAt
        };

        await context.Response.WriteAsJsonAsync(dto);
    }

    private static async Task GetAllScopesHandler(IScopeService scopeService, HttpContext context)
    {
        var scopes = await scopeService.GetAllScopesAsync();

        var dtos = scopes.Select(s => new ScopeDto
        {
            Id = s.Id,
            Name = s.Name,
            DisplayName = s.DisplayName,
            Description = s.Description,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        }).ToList();

        await context.Response.WriteAsJsonAsync(dtos);
    }

    // ========== Audit Log Handlers ==========

    private static async Task GetAuditLogsHandler(
        IAuditLogService auditLogService,
        HttpContext context,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var logs = await auditLogService.GetAllAuditLogsAsync(skip, take);

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

        await context.Response.WriteAsJsonAsync(dtos);
    }

    private static async Task GetUserAuditLogsHandler(
        Guid userId,
        IAuditLogService auditLogService,
        HttpContext context,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var logs = await auditLogService.GetUserAuditLogsAsync(userId, skip, take);

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

        await context.Response.WriteAsJsonAsync(dtos);
    }

    // ========== User Handlers ==========

    private static async Task GetAllUsersHandler(
        IUserRepository userRepository,
        HttpContext context,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var users = await userRepository.GetAllAsync(skip, take);

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

        await context.Response.WriteAsJsonAsync(dtos);
    }
}

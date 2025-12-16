namespace IdentityService.Application.DTOs;

public class CreateClientRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ClientType { get; set; } = "Confidential";
    public List<string>? RedirectUris { get; set; }
    public List<string>? AllowedOrigins { get; set; }
    public List<string> AllowedScopeNames { get; set; } = [];
    public int AccessTokenLifetime { get; set; } = 3600;
    public int RefreshTokenLifetime { get; set; } = 2592000;
    public bool AllowRefreshTokenRotation { get; set; } = true;
}

public class ClientDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ClientType { get; set; } = null!;
    public bool IsActive { get; set; }
    public int AccessTokenLifetime { get; set; }
    public int RefreshTokenLifetime { get; set; }
    public List<string> AllowedScopes { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class ClientSecretDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!; // Only shown once
    public string Name { get; set; } = null!;
}

public class CreateScopeRequest
{
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
}

public class ScopeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

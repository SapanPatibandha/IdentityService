namespace IdentityService.Core.Entities;

public class Client
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!; // Hashed
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ClientType { get; set; } = "Confidential"; // Public or Confidential
    public bool IsActive { get; set; } = true;
    public string? RedirectUris { get; set; } // JSON array of URIs
    public string? AllowedOrigins { get; set; } // CORS whitelist
    public int AccessTokenLifetime { get; set; } = 3600; // 1 hour in seconds
    public int RefreshTokenLifetime { get; set; } = 2592000; // 30 days in seconds
    public bool AllowRefreshTokenRotation { get; set; } = true;
    public bool RequireConsent { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public ICollection<Scope> AllowedScopes { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}

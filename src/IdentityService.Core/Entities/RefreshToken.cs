namespace IdentityService.Core.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public string Token { get; set; } = null!;
    public string? RotatedToken { get; set; } // Previous token during rotation
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;

    // Navigation properties
    public User User { get; set; } = null!;
}

namespace IdentityService.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public string Action { get; set; } = null!; // e.g., "USER_LOGIN", "TOKEN_ISSUED", "2FA_VERIFIED"
    public string Resource { get; set; } = null!; // e.g., "User", "Client", "Token"
    public string? Description { get; set; }
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Client? Client { get; set; }
}

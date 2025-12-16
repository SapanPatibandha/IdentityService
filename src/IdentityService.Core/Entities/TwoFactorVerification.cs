namespace IdentityService.Core.Entities;

public class TwoFactorVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Method { get; set; } = null!; // "email" or "totp"
    public string VerificationCode { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}

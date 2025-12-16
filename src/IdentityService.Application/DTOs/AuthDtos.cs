namespace IdentityService.Application.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public bool TwoFactorRequired { get; set; }
    public string? TwoFactorToken { get; set; }
}

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
    public string? ClientId { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string? RefreshToken { get; set; }
}

public class TwoFactorSetupResponse
{
    public string Secret { get; set; } = null!;
    public string QrCode { get; set; } = null!;
}

public class TwoFactorVerifyRequest
{
    public string Code { get; set; } = null!;
    public string? TwoFactorToken { get; set; }
    public string Method { get; set; } = "totp"; // "totp" or "email"
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

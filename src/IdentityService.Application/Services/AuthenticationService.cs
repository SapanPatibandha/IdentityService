using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using IdentityService.Core.Entities;
using IdentityService.Core.Interfaces;

namespace IdentityService.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorVerificationRepository _twoFactorRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    public AuthenticationService(
        IUserRepository userRepository,
        ITwoFactorVerificationRepository twoFactorRepository,
        IEmailService emailService,
        IAuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _twoFactorRepository = twoFactorRepository;
        _emailService = emailService;
        _auditLogService = auditLogService;
    }

    public async Task<(bool Success, User? User, string? ErrorMessage)> RegisterAsync(
        string username, string email, string password, string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, null, "Username, email, and password are required");

        if (password.Length < 8)
            return (false, null, "Password must be at least 8 characters");

        if (await _userRepository.ExistsAsync(username, email))
            return (false, null, "Username or email already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            EmailVerificationToken = GenerateToken(),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        try
        {
            await _userRepository.AddAsync(user);
            await _emailService.SendEmailVerificationAsync(email, user.EmailVerificationToken);
            return (true, user, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Registration failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, User? User, string? ErrorMessage)> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
            return (false, null, "Invalid username or password");

        if (user.IsLocked && user.LockoutUntil > DateTime.UtcNow)
            return (false, null, "Account is locked. Please try again later.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
            }
            await _userRepository.UpdateAsync(user);
            return (false, null, "Invalid username or password");
        }

        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return (true, user, null);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var users = await _userRepository.GetAllAsync(0, 1000);
        var user = users.FirstOrDefault(u => u.EmailVerificationToken == token);
        
        if (user == null || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            return false;

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<(bool Success, string? Token)> InitiateTwoFactorAsync(User user, string method)
    {
        try
        {
            string code = GenerateVerificationCode();
            var verification = new TwoFactorVerification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Method = method,
                VerificationCode = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                CreatedAt = DateTime.UtcNow,
                IsVerified = false
            };

            await _twoFactorRepository.AddAsync(verification);

            if (method == "email")
            {
                await _emailService.SendTwoFactorCodeAsync(user.Email, code);
            }

            // Return a temporary token for client to use in verification
            var tempToken = GenerateToken();
            return (true, tempToken);
        }
        catch
        {
            return (false, null);
        }
    }

    public async Task<(bool Success, string? Message)> VerifyTwoFactorAsync(User user, string code, string method)
    {
        var verification = await _twoFactorRepository.GetPendingByUserAndMethodAsync(user.Id, method);
        
        if (verification == null)
            return (false, "No pending verification found");

        if (verification.VerificationCode != code)
            return (false, "Invalid code");

        verification.IsVerified = true;
        verification.VerifiedAt = DateTime.UtcNow;
        await _twoFactorRepository.UpdateAsync(verification);

        return (true, "2FA verified successfully");
    }

    public async Task<(string Secret, string QrCode)> SetupTotpAsync(User user, string appName = "IdentityService")
    {
        // Generate a random secret
        byte[] secret = RandomNumberGenerator.GetBytes(20);
        
        var secretBase32 = Convert.ToBase64String(secret);
        
        // Format for QR code (using standard TOTP URI format)
        var provisioning_uri = $"otpauth://totp/{user.Email}?secret={secretBase32}&issuer={appName}";

        user.TwoFactorSecret = secretBase32;
        user.TwoFactorEnabled = true;
        await _userRepository.UpdateAsync(user);

        return (secretBase32, provisioning_uri);
    }

    public async Task<bool> ValidatePasswordAsync(string password, string passwordHash)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.Verify(password, passwordHash));
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.HashPassword(password));
    }

    private string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
}

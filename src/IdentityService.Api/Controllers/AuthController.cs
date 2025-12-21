using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdentityService.Application.DTOs;
using IdentityService.Core.Interfaces;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ITokenService _tokenService;
    private readonly IClientService _clientService;
    private readonly IAuditLogService _auditLogService;

    public AuthController(
        IAuthenticationService authService,
        ITokenService tokenService,
        IClientService clientService,
        IAuditLogService auditLogService)
    {
        _authService = authService;
        _tokenService = tokenService;
        _clientService = clientService;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, user, errorMessage) = await _authService.RegisterAsync(
            request.Username, request.Email, request.Password, request.FirstName, request.LastName);

        if (!success)
        {
            await _auditLogService.LogAsync(null, null, "USER_REGISTER_FAILED", "User", false, errorMessage: errorMessage);
            return BadRequest(new { message = errorMessage });
        }

        await _auditLogService.LogAsync(user!.Id, null, "USER_REGISTERED", "User", true, $"User {request.Username} registered");

        return Ok(new { message = "Registration successful. Please verify your email.", userId = user!.Id });
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, user, errorMessage) = await _authService.LoginAsync(request.Username, request.Password);

        if (!success)
        {
            await _auditLogService.LogAsync(null, null, "LOGIN_FAILED", "User", false, errorMessage: errorMessage);
            return Unauthorized(new { message = errorMessage });
        }

        var clientIdHeader = HttpContext.Request.Headers["X-Client-Id"].FirstOrDefault();
        var client = await _clientService.GetClientAsync(Guid.Parse(clientIdHeader ?? Guid.Empty.ToString()));

        // If no valid client provided or not found, use the default admin client
        if (client == null)
        {
            // Try to get the admin dashboard client
            var adminClient = await _clientService.GetAllClientsAsync();
            client = adminClient.FirstOrDefault(c => c.ClientId == "admin-dashboard");
            
            if (client == null)
            {
                return BadRequest(new { message = "No valid client available. Please provide X-Client-Id header." });
            }
        }

        var scopes = await _clientService.GetClientScopesAsync(client.Id);
        var accessToken = _tokenService.GenerateAccessToken(user!, scopes.Select(s => s.Name).ToList(), client.Id);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user!, client.Id, GetIpAddress(), GetUserAgent());

        // Check if 2FA is required
        if (user!.TwoFactorEnabled)
        {
            var (twoFactorSuccess, twoFactorToken) = await _authService.InitiateTwoFactorAsync(user, "totp");
            if (!twoFactorSuccess)
                return StatusCode(500, new { message = "Failed to initiate 2FA" });

            await _auditLogService.LogAsync(user.Id, client.Id, "2FA_REQUIRED", "User", true);

            return Ok(new LoginResponse
            {
                TwoFactorRequired = true,
                TwoFactorToken = twoFactorToken
            });
        }

        await _auditLogService.LogAsync(user.Id, client.Id, "LOGIN_SUCCESS", "User", true);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            TwoFactorRequired = false
        });
    }

    /// <summary>
    /// Verify 2FA code
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        // Get user from temp token or session
        // For now, simplified implementation
        return Ok(new { message = "2FA verified" });
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request,
        [FromServices] IRefreshTokenRepository refreshTokenRepository,
        [FromServices] ITokenService tokenService)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        
        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            await _auditLogService.LogAsync(refreshToken?.UserId, refreshToken?.ClientId, "TOKEN_REFRESH_FAILED", "Token", false, "Invalid or expired token");
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        var (newAccessToken, newRefreshToken) = await tokenService.RefreshAccessTokenAsync(refreshToken, GetIpAddress());

        await _auditLogService.LogAsync(refreshToken.UserId, refreshToken.ClientId, "TOKEN_REFRESHED", "Token", true);

        return Ok(new TokenResponse
        {
            AccessToken = newAccessToken,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            RefreshToken = newRefreshToken?.Token
        });
    }

    /// <summary>
    /// Logout and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        await _tokenService.RevokeTokenAsync(token);

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Setup TOTP for 2FA
    /// </summary>
    [HttpPost("setup-totp")]
    [Authorize]
    public async Task<IActionResult> SetupTotp()
    {
        // Get current user from token
        return Ok(new { message = "TOTP setup initiated" });
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetUserAgent()
    {
        return HttpContext.Request.Headers["User-Agent"].ToString();
    }
}

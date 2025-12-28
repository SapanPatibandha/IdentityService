using Microsoft.AspNetCore.Mvc;
using IdentityService.Application.DTOs;
using IdentityService.Core.Interfaces;

namespace IdentityService.Api.Extensions;

/// <summary>
/// Extension methods for registering Auth endpoints
/// </summary>
public static class AuthEndpointsExtensions
{
    public static void MapAuthEndpoints(this WebApplication app, RouteGroupBuilder apiGroup)
    {
        var authGroup = apiGroup.MapGroup("/auth");

        authGroup.MapPost("/register", RegisterHandler)
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Register a new user");

        authGroup.MapPost("/login", LoginHandler)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Login with username and password");

        authGroup.MapPost("/verify-2fa", VerifyTwoFactorHandler)
            .AllowAnonymous()
            .WithName("VerifyTwoFactor")
            .WithSummary("Verify 2FA code");

        authGroup.MapPost("/refresh", RefreshTokenHandler)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithSummary("Refresh access token");

        authGroup.MapPost("/logout", LogoutHandler)
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Logout and revoke tokens");

        authGroup.MapPost("/setup-totp", SetupTotpHandler)
            .RequireAuthorization()
            .WithName("SetupTotp")
            .WithSummary("Setup TOTP for 2FA");
    }

    // ========== Auth Handlers ==========

    private static async Task RegisterHandler(
        RegisterRequest request,
        IAuthenticationService authService,
        IAuditLogService auditLogService,
        HttpContext context)
    {
        var (success, user, errorMessage) = await authService.RegisterAsync(
            request.Username, request.Email, request.Password, request.FirstName, request.LastName);

        if (!success)
        {
            await auditLogService.LogAsync(null, null, "USER_REGISTER_FAILED", "User", false, errorMessage: errorMessage);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { message = errorMessage });
            return;
        }

        await auditLogService.LogAsync(user!.Id, null, "USER_REGISTERED", "User", true, $"User {request.Username} registered");
        context.Response.StatusCode = 200;
        await context.Response.WriteAsJsonAsync(new { message = "Registration successful. Please verify your email.", userId = user!.Id });
    }

    private static async Task LoginHandler(
        LoginRequest request,
        IAuthenticationService authService,
        ITokenService tokenService,
        IClientService clientService,
        IAuditLogService auditLogService,
        HttpContext context)
    {
        var (success, user, errorMessage) = await authService.LoginAsync(request.Username, request.Password);

        if (!success)
        {
            await auditLogService.LogAsync(null, null, "LOGIN_FAILED", "User", false, errorMessage: errorMessage);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = errorMessage });
            return;
        }

        var clientIdHeader = context.Request.Headers["X-Client-Id"].FirstOrDefault();
        var client = await clientService.GetClientAsync(Guid.Parse(clientIdHeader ?? Guid.Empty.ToString()));

        if (client == null)
        {
            var adminClient = await clientService.GetAllClientsAsync();
            client = adminClient.FirstOrDefault(c => c.ClientId == "admin-dashboard");
            
            if (client == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { message = "No valid client available. Please provide X-Client-Id header." });
                return;
            }
        }

        var scopes = await clientService.GetClientScopesAsync(client.Id);
        var accessToken = tokenService.GenerateAccessToken(user!, scopes.Select(s => s.Name).ToList(), client.Id);
        var refreshToken = await tokenService.GenerateRefreshTokenAsync(user!, client.Id, GetIpAddress(context), GetUserAgent(context));

        if (user!.TwoFactorEnabled)
        {
            var (twoFactorSuccess, twoFactorToken) = await authService.InitiateTwoFactorAsync(user, "totp");
            if (!twoFactorSuccess)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { message = "Failed to initiate 2FA" });
                return;
            }

            await auditLogService.LogAsync(user.Id, client.Id, "2FA_REQUIRED", "User", true);
            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(new LoginResponse
            {
                TwoFactorRequired = true,
                TwoFactorToken = twoFactorToken
            });
            return;
        }

        await auditLogService.LogAsync(user.Id, client.Id, "LOGIN_SUCCESS", "User", true);

        await context.Response.WriteAsJsonAsync(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            TwoFactorRequired = false
        });
    }

    private static async Task VerifyTwoFactorHandler(TwoFactorVerifyRequest request, HttpContext context)
    {
        await context.Response.WriteAsJsonAsync(new { message = "2FA verified" });
    }

    private static async Task RefreshTokenHandler(
        RefreshTokenRequest request,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IAuditLogService auditLogService,
        HttpContext context)
    {
        var refreshToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        
        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            await auditLogService.LogAsync(refreshToken?.UserId, refreshToken?.ClientId, "TOKEN_REFRESH_FAILED", "Token", false, "Invalid or expired token");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid or expired refresh token" });
            return;
        }

        var (newAccessToken, newRefreshToken) = await tokenService.RefreshAccessTokenAsync(refreshToken, GetIpAddress(context));

        await auditLogService.LogAsync(refreshToken.UserId, refreshToken.ClientId, "TOKEN_REFRESHED", "Token", true);

        await context.Response.WriteAsJsonAsync(new TokenResponse
        {
            AccessToken = newAccessToken,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            RefreshToken = newRefreshToken?.Token
        });
    }

    private static async Task LogoutHandler(
        ITokenService tokenService,
        HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        await tokenService.RevokeTokenAsync(token);
        await context.Response.WriteAsJsonAsync(new { message = "Logged out successfully" });
    }

    private static async Task SetupTotpHandler(HttpContext context)
    {
        await context.Response.WriteAsJsonAsync(new { message = "TOTP setup initiated" });
    }

    // ========== Helper Methods ==========

    private static string GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].ToString();
    }
}

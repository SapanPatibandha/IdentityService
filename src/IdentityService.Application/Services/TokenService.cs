using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Core.Entities;
using IdentityService.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IdentityService.Application.Services;

public class TokenService : ITokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository, IConfiguration configuration, SymmetricSecurityKey signingKey)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _configuration = configuration;
        _signingKey = signingKey;
    }

    public string GenerateAccessToken(User user, List<string> scopes, Guid clientId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("username", user.Username),
            new Claim("email", user.Email),
            new Claim("client_id", clientId.ToString())
        };

        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"] ?? "identityservice",
            Audience = _configuration["Jwt:Audience"] ?? "identityservice-api",
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(User user, Guid clientId, string ipAddress, string userAgent)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClientId = clientId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _refreshTokenRepository.AddAsync(refreshToken);
        return refreshToken;
    }

    public async Task<(string AccessToken, RefreshToken? NewRefreshToken)> RefreshAccessTokenAsync(RefreshToken currentRefreshToken, string ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(currentRefreshToken.UserId);
        if (user == null) throw new InvalidOperationException("User not found");

        var accessToken = GenerateAccessToken(user, new List<string>(), currentRefreshToken.ClientId);
        RefreshToken? newRefreshToken = null;

        if (currentRefreshToken.ExpiresAt < DateTime.UtcNow.AddDays(7))
        {
            newRefreshToken = await GenerateRefreshTokenAsync(user, currentRefreshToken.ClientId, ipAddress, "");
            await _refreshTokenRepository.UpdateAsync(currentRefreshToken);
        }

        return (accessToken, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string token)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
        if (refreshToken != null)
        {
            await _refreshTokenRepository.RevokeAsync(refreshToken.Id, "Revoked by user");
        }
    }

    public (Guid UserId, List<string> Scopes, Guid ClientId) ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "identityservice",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "identityservice-api",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userId = Guid.Parse(principal.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Invalid token"));
            var clientId = Guid.Parse(principal.FindFirst("client_id")?.Value ?? throw new InvalidOperationException("Invalid token"));
            var scopes = principal.FindAll("scope").Select(c => c.Value).ToList();

            return (userId, scopes, clientId);
        }
        catch
        {
            throw new InvalidOperationException("Invalid token");
        }
    }
}


using ApplyFlow.Server.Configuration;
using ApplyFlow.Server.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ApplyFlow.Server.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(
        int userId,
        string fullName,
        string email,
        string role)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(
            _settings.AccessTokenExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return (token, expiresAt);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        var token = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var expiresAt = DateTime.UtcNow.AddDays(
            _settings.RefreshTokenExpirationDays);

        return (token, expiresAt);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}

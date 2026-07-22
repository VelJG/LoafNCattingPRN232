using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LoafNCatting.Services.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        ValidateSettings(_settings);
    }

    public JwtTokenResult CreateToken(User user, string roleName)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(AuthClaimTypes.Role, roleName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(now).ToString(),
                ClaimValueTypes.Integer64)
        };
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }

    private static void ValidateSettings(JwtSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Issuer) ||
            string.IsNullOrWhiteSpace(settings.Audience) ||
            string.IsNullOrWhiteSpace(settings.SigningKey) ||
            Encoding.UTF8.GetByteCount(settings.SigningKey) < 32 ||
            settings.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT settings require issuer, audience, a signing key of at least 32 bytes, and a positive access-token lifetime.");
        }
    }
}

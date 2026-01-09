using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BankMore.JwtService.Security.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BankMore.JwtService.Security;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public TokenResult GenerateToken(Guid id)
    {
        var now = DateTime.Now;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new TokenResult
        {
            AccessToken = accessToken,
            RefreshToken = GenerateRefreshTokenString(),
            AccessTokenExpiresAt = jwt.ValidTo
        };
    }

    private static string GenerateRefreshTokenString()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, parameters, out var validatedToken);

        if (validatedToken is not JwtSecurityToken jwt ||
            !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            throw new SecurityTokenException("Token inválido.");

        return principal;
    }
}

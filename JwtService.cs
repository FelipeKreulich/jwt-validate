using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtSettings
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryMinutes { get; set; }
}

public class JwtService
{
    private readonly JwtSettings _settings;
    private readonly byte[] _secretBytes;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtService(JwtSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _secretBytes = Encoding.UTF8.GetBytes(_settings.Secret);

        var securityKey = new SymmetricSecurityKey(_secretBytes);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,

            ValidateAudience = true,
            ValidAudience = _settings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    public string GenerateToken(string subject, IDictionary<string, string>? extraClaims = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        if (extraClaims != null)
        {
            foreach (var kv in extraClaims)
                claims.Add(new Claim(kv.Key, kv.Value));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (bool IsValid, ClaimsPrincipal? Principal, string? Error) ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(
                token,
                _validationParameters,
                out var validatedToken
            );

            if (
                !(validatedToken is JwtSecurityToken jwt)
                || !jwt.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                return (false, null, "Algoritmo de assinatura inválido.");
            }

            return (true, principal, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}

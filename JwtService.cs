using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class JwtSettings
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiryMinutes { get; set; }
    public string Algorithm { get; set; } = "HS256"; // default
    public string? PrivateKeyPath { get; set; }
    public string? PublicKeyPath { get; set; }
}

public class JwtService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtService(JwtSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // Escolhe a chave e algoritmo com base nas configurações
        SecurityKey signingKey;

        switch (_settings.Algorithm.ToUpper())
        {
            case "RS256":
                if (
                    string.IsNullOrEmpty(_settings.PrivateKeyPath)
                    || string.IsNullOrEmpty(_settings.PublicKeyPath)
                )
                    throw new Exception("RSA key paths must be specified for RS256 algorithm.");

                // Importa a chave privada (para assinar)
                var privateKeyPem = File.ReadAllText(_settings.PrivateKeyPath);
                var rsaPrivate = RSA.Create();
                rsaPrivate.ImportFromPem(privateKeyPem);

                signingKey = new RsaSecurityKey(rsaPrivate);
                _signingCredentials = new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.RsaSha256
                );
                break;

            case "HS512":
                var secret512 = Encoding.UTF8.GetBytes(_settings.Secret);
                signingKey = new SymmetricSecurityKey(secret512);
                _signingCredentials = new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha512
                );
                break;

            case "HS256":
            default:
                var secret = Encoding.UTF8.GetBytes(_settings.Secret);
                signingKey = new SymmetricSecurityKey(secret);
                _signingCredentials = new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256
                );
                break;
        }

        // Configuração de validação
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,

            ValidateAudience = true,
            ValidAudience = _settings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetValidationKey(_settings),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    // Retorna a chave correta para validação dependendo do algoritmo
    private SecurityKey GetValidationKey(JwtSettings settings)
    {
        switch (settings.Algorithm.ToUpper())
        {
            case "RS256":
                var publicKeyPem = File.ReadAllText(settings.PublicKeyPath!);
                var rsaPublic = RSA.Create();
                rsaPublic.ImportFromPem(publicKeyPem);
                return new RsaSecurityKey(rsaPublic);

            case "HS512":
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));

            case "HS256":
            default:
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        }
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
                    _signingCredentials.Algorithm,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                return (false, null, "Invalid signing algorithm.");
            }

            return (true, principal, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}

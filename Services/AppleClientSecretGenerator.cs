using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GloryLikeWebApp.Services;

public interface IAppleClientSecretGenerator
{
    string CreateClientSecret();
}

public sealed class AppleClientSecretGenerator
    : IAppleClientSecretGenerator
{
    private readonly IConfiguration _configuration;

    public AppleClientSecretGenerator(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateClientSecret()
    {
        var clientId = RequiredSetting("ClientId");
        var teamId = RequiredSetting("TeamId");
        var keyId = RequiredSetting("KeyId");
        var privateKey = ReadPrivateKey();

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKey);

        var securityKey = new ECDsaSecurityKey(ecdsa)
        {
            KeyId = keyId
        };
        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.EcdsaSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: teamId,
            audience: "https://appleid.apple.com",
            claims:
            [
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    clientId),
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(now)
                        .ToUnixTimeSeconds()
                        .ToString(
                            CultureInfo.InvariantCulture),
                    ClaimValueTypes.Integer64)
            ],
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(5),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private string RequiredSetting(string key)
    {
        var value = _configuration[
            $"Authentication:Apple:{key}"];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Authentication:Apple:{key} konfiqurasiya edilməyib.");
        }

        return value.Trim();
    }

    private string ReadPrivateKey()
    {
        var base64Value = _configuration[
            "Authentication:Apple:PrivateKeyBase64"];

        if (!string.IsNullOrWhiteSpace(base64Value))
        {
            try
            {
                return Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                        base64Value.Trim()));
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    "Authentication:Apple:PrivateKeyBase64 düzgün Base64 deyil.",
                    exception);
            }
        }

        return RequiredSetting("PrivateKey")
            .Replace("\\n", "\n", StringComparison.Ordinal);
    }
}

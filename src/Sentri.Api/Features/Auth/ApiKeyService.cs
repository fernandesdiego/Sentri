using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Sentri.Api.Domain;
using System.Security.Cryptography;

namespace Sentri.Api.Features.Auth;

public class ApiKeyService(IPasswordHasher<User> passwordHasher)
{
    public Guid GenerateKeyId() => Guid.NewGuid();

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public string BuildRawKey(Guid keyId, string secret)
        => $"{AuthConstants.ApiKeyPrefix}_{keyId:N}.{secret}";

    public string HashSecret(string secret)
        => passwordHasher.HashPassword(null!, secret);

    public bool VerifySecret(string hash, string secret)
        => passwordHasher.VerifyHashedPassword(null!, hash, secret) != PasswordVerificationResult.Failed;

    public bool TryParseRawKey(string rawKey, out Guid keyId, out string secret)
    {
        keyId = Guid.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(rawKey) || !rawKey.StartsWith($"{AuthConstants.ApiKeyPrefix}_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var payload = rawKey[(AuthConstants.ApiKeyPrefix.Length + 1)..];
        var separatorIndex = payload.IndexOf('.');

        if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
        {
            return false;
        }

        var keyIdPart = payload[..separatorIndex];
        var secretPart = payload[(separatorIndex + 1)..];

        if (!Guid.TryParseExact(keyIdPart, "N", out keyId))
        {
            return false;
        }

        secret = secretPart;
        return true;
    }
}
using System.ComponentModel.DataAnnotations;

namespace Sentri.Api.Domain;

public class ApiKey : Entity
{
    public Guid Id { get; init; }

    public Guid UserId { get; private set; }

    [MaxLength(100)]
    public string? Name { get; private set; }

    [MaxLength(255)]
    public string SecretHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastUsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public User User { get; private set; } = null!;

    public ApiKey(Guid id, Guid userId, string secretHash, string? name = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("API key id is required.");
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.");
        if (string.IsNullOrWhiteSpace(secretHash)) throw new ArgumentException("Secret hash is required.");

        Id = id;
        UserId = userId;
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        SecretHash = secretHash;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkUsed()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        RevokedAt ??= DateTimeOffset.UtcNow;
    }
}
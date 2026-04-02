using System.ComponentModel.DataAnnotations;

namespace Sentri.Api.Domain;

public class User
{
    public Guid Id { get; init; }
    
    [MaxLength(255)]
    public string Email { get; private set; }
    
    [MaxLength(100)]
    public string Name { get; private set; }
    
    public string PasswordHash { get; private set; }

    // Navigation property
    public ICollection<Provider> Providers { get; private set; } = new List<Provider>();

    public User(string email, string name, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.");

        Id = Guid.NewGuid();
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
    }
}

using Pedidos.Domain.Common;

namespace Pedidos.Domain.Entities;

/// <summary>
/// Usuário que autentica no sistema. A senha nunca é armazenada em texto puro —
/// apenas o hash (ver IPasswordHasher na camada de Application).
/// </summary>
public class User : Entity
{
    /// <summary>Nome de exibição.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>E-mail (login, único).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Hash da senha.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Papel do usuário (ex.: "admin", "user").</summary>
    public string Role { get; private set; } = "user";

    /// <summary>Data de criação (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(string name, string email, string passwordHash, string role, DateTime createdAtUtc)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAtUtc;
    }
}

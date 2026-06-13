using Pedidos.Domain.Common;

namespace Pedidos.Domain.Entities;

/// <summary>
/// Cliente que realiza pedidos. Mantido simples de propósito — o foco do desafio
/// está na arquitetura, não na complexidade do domínio.
/// </summary>
public class Customer : Entity
{
    /// <summary>Nome do cliente.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>E-mail de contato (único).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Data de criação do registro (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    private Customer() { }

    public Customer(string name, string email, DateTime createdAtUtc)
    {
        Name = name;
        Email = email;
        CreatedAt = createdAtUtc;
    }
}

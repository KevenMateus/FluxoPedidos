namespace Pedidos.Domain.Common;

/// <summary>
/// Classe base para entidades do domínio. Garante identidade por <see cref="Id"/>.
/// </summary>
public abstract class Entity
{
    /// <summary>Identificador único da entidade.</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

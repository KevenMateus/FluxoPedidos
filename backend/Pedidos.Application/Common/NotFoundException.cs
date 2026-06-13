namespace Pedidos.Application.Common;

/// <summary>
/// Lançada quando uma entidade referenciada não existe. A API a traduz para 404/400.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

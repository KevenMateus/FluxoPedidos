namespace Pedidos.Application.Common;

/// <summary>Lançada quando as credenciais são inválidas. A API a traduz para 401.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

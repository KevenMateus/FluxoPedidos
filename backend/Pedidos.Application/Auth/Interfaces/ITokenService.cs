using Pedidos.Application.Auth.Dtos;

namespace Pedidos.Application.Auth.Interfaces;

/// <summary>Porta para emissão de tokens. A implementação JWT vive na Infrastructure.</summary>
public interface ITokenService
{
    /// <summary>Gera um token para o usuário e devolve o token e seu vencimento (UTC).</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(UserDto user);
}

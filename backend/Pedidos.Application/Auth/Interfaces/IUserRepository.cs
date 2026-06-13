using Pedidos.Application.Auth.Dtos;

namespace Pedidos.Application.Auth.Interfaces;

/// <summary>
/// Repositório de usuários (lado de leitura para autenticação). Implementado com
/// EF Core + LINQ — busca direta por e-mail, sem agregação.
/// </summary>
public interface IUserRepository
{
    /// <summary>Busca o usuário pelo e-mail, incluindo o hash da senha (uso interno do AuthService).</summary>
    Task<UserCredentialsDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

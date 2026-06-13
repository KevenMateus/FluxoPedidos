using Pedidos.Application.Auth.Dtos;

namespace Pedidos.Application.Auth.Interfaces;

/// <summary>Casos de uso de autenticação.</summary>
public interface IAuthService
{
    /// <summary>Autentica por e-mail/senha e devolve o token. Lança UnauthorizedException se inválido.</summary>
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

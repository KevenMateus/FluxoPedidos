using Pedidos.Application.Auth.Dtos;
using Pedidos.Application.Auth.Interfaces;
using Pedidos.Application.Common;

namespace Pedidos.Application.Auth.Services;

/// <summary>
/// Implementação do login. Orquestra o repositório de usuários e as portas de
/// hashing e de token — trafegando apenas DTOs.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var credentials = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (credentials is null || !_passwordHasher.Verify(request.Password, credentials.PasswordHash))
            throw new UnauthorizedException("E-mail ou senha inválidos.");

        var user = new UserDto
        {
            Id = credentials.Id,
            Name = credentials.Name,
            Email = credentials.Email,
            Role = credentials.Role
        };

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new AuthResultDto { Token = token, ExpiresAtUtc = expiresAt, User = user };
    }
}

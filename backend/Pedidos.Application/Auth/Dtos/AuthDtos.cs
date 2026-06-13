using System.ComponentModel.DataAnnotations;

namespace Pedidos.Application.Auth.Dtos;

/// <summary>Credenciais de login.</summary>
public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>Usuário como exposto pela API (sem dados sensíveis).</summary>
public class UserDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

/// <summary>Resultado de um login bem-sucedido.</summary>
public class AuthResultDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public UserDto User { get; init; } = new();
}

/// <summary>
/// DTO interno (não exposto pela API) com o hash da senha, usado apenas pelo
/// AuthService para verificar credenciais. Mantém a regra "service só toca DTO"
/// — o hash trafega num DTO, nunca na entidade de domínio.
/// </summary>
public class UserCredentialsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
}

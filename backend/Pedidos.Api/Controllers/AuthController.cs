using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Application.Auth.Dtos;
using Pedidos.Application.Auth.Interfaces;

namespace Pedidos.Api.Controllers;

/// <summary>Autenticação (login JWT).</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Autentica por e-mail/senha e devolve um token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken = default)
        => Ok(await _authService.LoginAsync(request, cancellationToken));

    /// <summary>Retorna os dados do usuário autenticado (a partir do token).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserDto> Me()
    {
        return Ok(new UserDto
        {
            Id = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty,
            Name = User.Identity?.Name ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
        });
    }
}

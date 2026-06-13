using System.Text.Json;
using Pedidos.Application.Common;

namespace Pedidos.Api;

/// <summary>
/// Traduz exceções da aplicação em respostas ProblemDetails consistentes,
/// mantendo os controllers limpos de try/catch.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Não autorizado", ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Operação inválida", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado.");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.io/{status}",
            title,
            status,
            detail
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

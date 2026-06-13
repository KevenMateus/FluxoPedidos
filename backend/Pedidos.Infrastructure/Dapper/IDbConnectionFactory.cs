using System.Data;

namespace Pedidos.Infrastructure.Dapper;

/// <summary>
/// Fábrica de conexões para o acesso via Dapper, desacoplando os repositórios
/// de leitura do provider concreto (Npgsql).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Cria e abre uma conexão pronta para uso.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

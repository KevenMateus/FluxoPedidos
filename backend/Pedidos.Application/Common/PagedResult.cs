namespace Pedidos.Application.Common;

/// <summary>
/// Resultado paginado genérico devolvido pelos endpoints de listagem.
/// </summary>
/// <typeparam name="T">Tipo do item da página.</typeparam>
public class PagedResult<T>
{
    /// <summary>Itens da página atual.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Página atual (base 1).</summary>
    public int Page { get; init; }

    /// <summary>Tamanho da página.</summary>
    public int PageSize { get; init; }

    /// <summary>Total de registros existentes (não apenas da página).</summary>
    public long TotalCount { get; init; }

    /// <summary>Total de páginas calculado a partir de <see cref="TotalCount"/>.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

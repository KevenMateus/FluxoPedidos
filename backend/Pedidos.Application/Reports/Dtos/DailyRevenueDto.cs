namespace Pedidos.Application.Reports.Dtos;

/// <summary>Faturamento agregado de um único dia.</summary>
public class DailyRevenueDto
{
    /// <summary>Dia (sem componente de hora).</summary>
    public DateOnly Date { get; init; }

    /// <summary>Quantidade de pedidos no dia.</summary>
    public int OrderCount { get; init; }

    /// <summary>Faturamento total do dia.</summary>
    public decimal Revenue { get; init; }
}

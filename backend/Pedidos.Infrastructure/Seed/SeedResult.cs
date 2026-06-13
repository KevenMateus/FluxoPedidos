namespace Pedidos.Infrastructure.Seed;

/// <summary>Resumo do que foi gerado em uma execução de seed.</summary>
public record SeedResult(int CustomersCreated, int ProductsCreated, int OrdersCreated, int OrderItemsCreated, long ElapsedMs);

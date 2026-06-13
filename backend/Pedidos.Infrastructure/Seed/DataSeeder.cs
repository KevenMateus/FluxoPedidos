using System.Diagnostics;
using System.Globalization;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Pedidos.Application.Auth.Interfaces;
using Pedidos.Domain.Entities;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Seed;

/// <summary>
/// Popula o banco com dados realistas. A massa de clientes/produtos vai por EF
/// (volume pequeno). Os pedidos e itens — que precisam de VOLUME para a
/// performance importar — vão por COPY binário do Npgsql, ordens de magnitude
/// mais rápido que INSERTs um a um.
/// </summary>
public class DataSeeder
{
    private const int CustomerCount = 2_000;
    private const int ProductCount = 300;
    private const int CopyBatchSize = 10_000;

    private const int RandomSeed = 20260613;

    private readonly AppDbContext _db;
    private readonly string _connectionString;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext db, string connectionString, IPasswordHasher passwordHasher, ILogger<DataSeeder> logger)
    {
        _db = db;
        _connectionString = connectionString;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Garante o schema e uma massa inicial. Idempotente: se já houver pedidos,
    /// não faz nada. Chamado no startup da API.
    /// </summary>
    public async Task<SeedResult> EnsureSeededAsync(int initialOrders = 5_000, CancellationToken cancellationToken = default)
    {
        await _db.Database.EnsureCreatedAsync(cancellationToken);

        await EnsureUsersAsync(cancellationToken);

        if (await _db.Orders.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Banco já populado — seed inicial ignorado.");
            return new SeedResult(0, 0, 0, 0, 0);
        }

        var (customers, products) = await EnsureReferenceDataAsync(cancellationToken);
        var orders = await GenerateOrdersAsync(initialOrders, cancellationToken);

        return new SeedResult(customers, products, orders.Orders, orders.Items, orders.ElapsedMs);
    }

    /// <summary>
    /// Gera N pedidos adicionais para teste de performance. Reaproveita os
    /// clientes/produtos existentes (criando a massa de referência se preciso).
    /// </summary>
    public async Task<SeedResult> GenerateForPerformanceAsync(int orderCount, CancellationToken cancellationToken = default)
    {
        await _db.Database.EnsureCreatedAsync(cancellationToken);
        var (customers, products) = await EnsureReferenceDataAsync(cancellationToken);
        var result = await GenerateOrdersAsync(orderCount, cancellationToken);
        return new SeedResult(customers, products, result.Orders, result.Items, result.ElapsedMs);
    }

    private async Task EnsureUsersAsync(CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(cancellationToken))
            return;

        var users = new[]
        {
            new User("Administrador", "admin@pedidos.local", _passwordHasher.Hash("admin123"), "admin", DateTime.UtcNow),
            new User("Operador", "user@pedidos.local", _passwordHasher.Hash("user123"), "user", DateTime.UtcNow),
        };
        _db.Users.AddRange(users);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Usuários semeados: admin@pedidos.local / user@pedidos.local");
    }

    private async Task<(int customers, int products)> EnsureReferenceDataAsync(CancellationToken cancellationToken)
    {
        int customersCreated = 0, productsCreated = 0;

        if (!await _db.Customers.AnyAsync(cancellationToken))
        {
            var faker = new Faker("pt_BR") { Random = new Randomizer(RandomSeed) };
            var customers = new List<Customer>(CustomerCount);
            for (int i = 0; i < CustomerCount; i++)
            {
                var name = faker.Name.FullName();
                var email = faker.Internet.Email(uniqueSuffix: $"{i}");
                customers.Add(new Customer(name, email, DateTime.UtcNow));
            }
            _db.Customers.AddRange(customers);
            await _db.SaveChangesAsync(cancellationToken);
            customersCreated = customers.Count;
            _logger.LogInformation("{Count} clientes criados.", customersCreated);
        }

        if (!await _db.Products.AnyAsync(cancellationToken))
        {
            var faker = new Faker("pt_BR") { Random = new Randomizer(RandomSeed + 1) };
            var products = new List<Product>(ProductCount);
            for (int i = 0; i < ProductCount; i++)
            {
                var name = faker.Commerce.ProductName();
                var sku = $"SKU-{i:D5}";
                var price = decimal.Parse(faker.Commerce.Price(5, 2500), CultureInfo.InvariantCulture);
                products.Add(new Product(name, sku, price));
            }
            _db.Products.AddRange(products);
            await _db.SaveChangesAsync(cancellationToken);
            productsCreated = products.Count;
            _logger.LogInformation("{Count} produtos criados.", productsCreated);
        }

        return (customersCreated, productsCreated);
    }

    private async Task<(int Orders, int Items, long ElapsedMs)> GenerateOrdersAsync(int orderCount, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var customerIds = await _db.Customers.Select(c => c.Id).ToListAsync(cancellationToken);
        var products = await _db.Products
            .Select(p => new { p.Id, p.Name, p.UnitPrice })
            .ToListAsync(cancellationToken);

        if (customerIds.Count == 0 || products.Count == 0)
            throw new InvalidOperationException("Não há clientes/produtos para gerar pedidos.");

        var rnd = new Randomizer(RandomSeed + 2);
        var statuses = (OrderStatus[])Enum.GetValues(typeof(OrderStatus));
        var payments = (PaymentMethod[])Enum.GetValues(typeof(PaymentMethod));
        var now = DateTime.UtcNow;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        int totalOrders = 0, totalItems = 0;
        int remaining = orderCount;

        while (remaining > 0)
        {
            var batch = Math.Min(CopyBatchSize, remaining);

            var orderRows = new List<(Guid Id, Guid CustomerId, DateTime CreatedAt, int Status, int PaymentMethod)>(batch);
            var itemRows = new List<(Guid Id, Guid OrderId, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)>(batch * 4);
            var eventRows = new List<(Guid Id, Guid OrderId, int Type, string Description, string Source, DateTime OccurredAt)>(batch * 2);

            for (int i = 0; i < batch; i++)
            {
                var orderId = Guid.NewGuid();
                var customerId = customerIds[rnd.Number(0, customerIds.Count - 1)];
                var createdAt = now.AddDays(-rnd.Number(0, 365)).AddMinutes(-rnd.Number(0, 1440));
                var status = statuses[rnd.Number(0, statuses.Length - 1)];
                var payment = payments[rnd.Number(0, payments.Length - 1)];
                orderRows.Add((orderId, customerId, createdAt, (int)status, (int)payment));

                eventRows.Add((Guid.NewGuid(), orderId, (int)OrderEventType.Created, "Pedido criado.", "system", createdAt));
                if (status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
                    eventRows.Add((Guid.NewGuid(), orderId, (int)OrderEventType.PaymentReceived,
                        $"Pagamento confirmado ({Order.PaymentLabel(payment)}).", "system", createdAt.AddMinutes(rnd.Number(1, 120))));

                var itemCount = rnd.Number(1, 6);
                for (int j = 0; j < itemCount; j++)
                {
                    var p = products[rnd.Number(0, products.Count - 1)];
                    var qty = rnd.Number(1, 10);
                    itemRows.Add((Guid.NewGuid(), orderId, p.Id, p.Name, qty, p.UnitPrice));
                }
            }

            await CopyOrdersAsync(conn, orderRows, cancellationToken);
            await CopyOrderItemsAsync(conn, itemRows, cancellationToken);
            await CopyOrderEventsAsync(conn, eventRows, cancellationToken);

            totalOrders += orderRows.Count;
            totalItems += itemRows.Count;
            remaining -= batch;
            _logger.LogInformation("Seed de pedidos: {Done}/{Total}", totalOrders, orderCount);
        }

        sw.Stop();
        _logger.LogInformation("Gerados {Orders} pedidos e {Items} itens em {Ms} ms.", totalOrders, totalItems, sw.ElapsedMilliseconds);
        return (totalOrders, totalItems, sw.ElapsedMilliseconds);
    }

    private static async Task CopyOrdersAsync(NpgsqlConnection conn, List<(Guid Id, Guid CustomerId, DateTime CreatedAt, int Status, int PaymentMethod)> rows, CancellationToken ct)
    {
        await using var writer = await conn.BeginBinaryImportAsync(
            "COPY orders (id, customer_id, created_at, status, payment_method) FROM STDIN (FORMAT BINARY)", ct);

        foreach (var r in rows)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(r.Id, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(r.CustomerId, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, ct);
            await writer.WriteAsync(r.Status, NpgsqlDbType.Integer, ct);
            await writer.WriteAsync(r.PaymentMethod, NpgsqlDbType.Integer, ct);
        }

        await writer.CompleteAsync(ct);
    }

    private static async Task CopyOrderEventsAsync(NpgsqlConnection conn, List<(Guid Id, Guid OrderId, int Type, string Description, string Source, DateTime OccurredAt)> rows, CancellationToken ct)
    {
        await using var writer = await conn.BeginBinaryImportAsync(
            "COPY order_events (id, order_id, type, description, source, occurred_at) FROM STDIN (FORMAT BINARY)", ct);

        foreach (var r in rows)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(r.Id, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(r.OrderId, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(r.Type, NpgsqlDbType.Integer, ct);
            await writer.WriteAsync(r.Description, NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(r.Source, NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(DateTime.SpecifyKind(r.OccurredAt, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, ct);
        }

        await writer.CompleteAsync(ct);
    }

    private static async Task CopyOrderItemsAsync(NpgsqlConnection conn, List<(Guid Id, Guid OrderId, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> rows, CancellationToken ct)
    {
        await using var writer = await conn.BeginBinaryImportAsync(
            "COPY order_items (id, order_id, product_id, product_name, quantity, unit_price) FROM STDIN (FORMAT BINARY)", ct);

        foreach (var r in rows)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(r.Id, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(r.OrderId, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(r.ProductId, NpgsqlDbType.Uuid, ct);
            await writer.WriteAsync(r.ProductName, NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(r.Quantity, NpgsqlDbType.Integer, ct);
            await writer.WriteAsync(r.UnitPrice, NpgsqlDbType.Numeric, ct);
        }

        await writer.CompleteAsync(ct);
    }
}

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
    private const int CustomerCount = 3_000;
    private const int CopyBatchSize = 10_000;
    private const int RandomSeed = 20260613;

    private readonly AppDbContext _db;
    private readonly string _connectionString;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    private static readonly (string Name, string Sku, decimal Price)[] CatalogProducts =
    [
        // Engenharia Civil
        ("Cimento Portland CP-II 50kg",                 "ENG-001", 32.90m),
        ("Argamassa Polimérica AC-III 20kg",             "ENG-002", 28.50m),
        ("Tijolo Cerâmico Furado 9x14x19 (pct 50un)",   "ENG-003", 64.00m),
        ("Areia Grossa Lavada (saco 20kg)",              "ENG-004", 12.90m),
        ("Brita Calcária Nº1 (saco 20kg)",               "ENG-005", 14.50m),
        ("Vergalhão CA-50 Ø10mm Barra 12m",              "ENG-006", 87.00m),
        ("Tubo PVC Esgoto 100mm x 6m",                   "ENG-007", 58.90m),
        ("Tela Soldada Q-92 2,45x6m",                    "ENG-008", 145.00m),
        ("Impermeabilizante Neutrol 3,6L",               "ENG-009", 42.90m),
        ("Telha Cerâmica Colonial (pct 10un)",           "ENG-010", 79.00m),
        ("Cal Hidratada 20kg",                           "ENG-011", 22.50m),
        ("Tinta Látex PVA Branca 18L",                   "ENG-012", 138.00m),
        ("Selador Acrílico 18L",                         "ENG-013", 112.00m),
        ("Massa Corrida PVA 25kg",                       "ENG-014", 68.00m),
        ("Fio Elétrico Flexível 2,5mm 100m",             "ENG-015", 195.00m),
        ("Disjuntor Bipolar 20A",                        "ENG-016", 38.00m),
        ("Tomada 2P+T 10A Branca",                       "ENG-017", 14.90m),
        ("Interruptor Simples 10A Branco",               "ENG-018", 11.50m),
        ("Caixa D'água Polietileno 500L",                "ENG-019", 340.00m),
        ("Joelho PVC 90° 100mm",                         "ENG-020", 8.90m),
        ("Luva PVC Soldável 100mm",                      "ENG-021", 7.50m),
        ("Registro de Gaveta Bronze 1\"",                "ENG-022", 62.00m),
        ("Mangueira Trançada PVC 1/2\" 50m",             "ENG-023", 89.00m),
        ("Parafuso Chumbador Nylon M10 (pct 50un)",      "ENG-024", 28.00m),
        ("Prego com Cabeça 17x27 (kg)",                  "ENG-025", 15.90m),
        ("Rolo Lã de Carneiro 23cm",                     "ENG-026", 18.50m),
        ("Espátula de Aço Inox 20cm",                    "ENG-027", 22.00m),
        ("Lixa Massa Folha Grão 120 (pct 25un)",         "ENG-028", 36.00m),
        ("Serra Copo Bimetálica 64mm",                   "ENG-029", 54.90m),
        ("Nível de Bolha Alumínio 60cm",                 "ENG-030", 47.00m),
        ("Trena de Fibra 30m",                           "ENG-031", 33.50m),
        ("Capacete de Segurança Branco CA-31149",        "ENG-032", 28.90m),
        ("Bota PVC Cano Médio Preta Nº43",               "ENG-033", 75.00m),
        ("Silicone Estrutural Branco 300ml",             "ENG-034", 24.90m),
        ("Espuma Expansiva Vedante 340g",                "ENG-035", 32.00m),
        ("Fita Veda-Rosca PTFE 12mm x 50m",             "ENG-036", 6.90m),
        ("Rejunte Flexível Cinza Cimento 1kg",           "ENG-037", 9.50m),
        ("Argamassa de Assentamento AC-II 20kg",         "ENG-038", 27.00m),
        ("Tinta Primer Anticorrosivo 3,6L",              "ENG-039", 98.00m),
        ("Régua Alumínio para Acabamento 1,5m",          "ENG-040", 41.00m),

        // Roupas e Vestuário
        ("Camiseta Polo Masculina Azul Marinho M",       "VES-001", 89.90m),
        ("Camiseta Polo Masculina Branca G",             "VES-002", 89.90m),
        ("Camiseta Polo Feminina Rosa M",                "VES-003", 79.90m),
        ("Calça Jeans Slim Masculina 42",                "VES-004", 149.90m),
        ("Calça Jeans Skinny Feminina 38",               "VES-005", 139.90m),
        ("Calça Social Masculina Preta 44",              "VES-006", 189.90m),
        ("Blusa de Moletom Feminina Cinza M",            "VES-007", 119.90m),
        ("Jaqueta Corta-Vento Masculina Preta G",        "VES-008", 229.90m),
        ("Vestido Casual Floral Feminino M",             "VES-009", 129.90m),
        ("Short Tactel Masculino Azul Royal M",          "VES-010", 69.90m),
        ("Meia Cano Médio Chumbo (kit 3 pares)",         "VES-011", 34.90m),
        ("Cueca Boxer Microfibra (kit 3 unidades)",      "VES-012", 59.90m),
        ("Camiseta Regata Feminina Branca P",            "VES-013", 49.90m),
        ("Bermuda Jeans Masculina Escura 40",            "VES-014", 109.90m),
        ("Tênis Casual Branco Unissex 42",               "VES-015", 249.90m),
        ("Sapatênis Feminino Nude 36",                   "VES-016", 199.90m),
        ("Chinelo Slide Masculino Preto 41",             "VES-017", 59.90m),
        ("Meias Esportivas Cano Curto (kit 6 pares)",    "VES-018", 49.90m),
        ("Cinto Couro Legítimo Marrom 105cm",            "VES-019", 89.90m),
        ("Boné Aba Curva Preto Básico",                  "VES-020", 49.90m),
        ("Legging Fitness Feminina Preta P",             "VES-021", 99.90m),
        ("Top Fitness Feminino Verde M",                 "VES-022", 79.90m),
        ("Jaqueta Jeans Masculina Azul Médio M",         "VES-023", 219.90m),
        ("Camisa Social Manga Longa Branca G",           "VES-024", 169.90m),
        ("Conjunto Pijama Listrado Feminino M",          "VES-025", 99.90m),
        ("Calçinha Algodão Conforto (kit 5 unidades)",   "VES-026", 69.90m),
        ("Mochila Escolar Preta Resistente 30L",         "VES-027", 149.90m),
        ("Bolsa Feminina Tiracolo Caramelo",             "VES-028", 189.90m),
        ("Carteira Couro Masculina Preta",               "VES-029", 119.90m),
        ("Óculos de Sol Unissex UV400",                  "VES-030", 159.90m),

        // Logística e Armazenagem
        ("Caixa de Papelão Duplo 40x30x30cm (pct 10un)", "LOG-001", 62.00m),
        ("Caixa de Papelão Triplo 60x40x40cm (pct 5un)", "LOG-002", 58.00m),
        ("Fita Adesiva Transparente 45mm x 100m (pct 6)", "LOG-003", 48.90m),
        ("Fita Adesiva Kraft 48mm x 50m (pct 4 rolos)",  "LOG-004", 39.90m),
        ("Lacre de Segurança Numerado (pct 100un)",      "LOG-005", 32.00m),
        ("Pallet PVC 1200x1000mm Carga 1T",              "LOG-006", 420.00m),
        ("Etiqueta Adesiva 100x150mm Rolo 500un",        "LOG-007", 55.90m),
        ("Filme Stretch 500mm x 300m 17 Micras",         "LOG-008", 78.00m),
        ("Envelope Plástico Segurança 32x40cm (pct 100)", "LOG-009", 42.00m),
        ("Saco de Ráfia Polipropileno 60x90cm (pct 50)", "LOG-010", 68.00m),
        ("Cinta Poliéster 19mm x 850m Azul",             "LOG-011", 185.00m),
        ("Esticador de Cinta Manual com Catraca",        "LOG-012", 129.00m),
        ("Balança de Piso Industrial Inox 300kg",        "LOG-013", 890.00m),
        ("Paleteira Manual Hidráulica 2500kg",           "LOG-014", 1250.00m),
        ("Prateleira Porta-Pallet 2,7m Alt x 2,7m Larg","LOG-015", 980.00m),
        ("Leitor de Código de Barras USB 1D/2D",        "LOG-016", 249.00m),
        ("Impressora de Etiquetas Térmica 4\"",          "LOG-017", 1490.00m),
        ("Cadeado de Alta Segurança Inox 40mm",          "LOG-018", 68.90m),
        ("Rastreador GPS Veicular Magnético",            "LOG-019", 350.00m),
        ("Caixa Organizadora Empilhável Tampa 60L",      "LOG-020", 89.00m),
        ("Canivete de Segurança Retrátil EPI",           "LOG-021", 24.90m),
        ("Colete Identificador Laranja Refletivo",       "LOG-022", 45.00m),
        ("Protetor de Canto Papelão (pct 50un)",         "LOG-023", 38.00m),
        ("Lanterna LED Industrial Recarregável 10W",     "LOG-024", 159.00m),
        ("Placa de Identificação de Prateleira A4",      "LOG-025", 12.90m),
        ("Fivela de Emenda Aço para Cinta 19mm (50un)", "LOG-026", 29.00m),
        ("Porta-Bobina Suporte de Chão para Stretch",    "LOG-027", 145.00m),
        ("Lacre Plástico Inviolável Contêiner (50un)",   "LOG-028", 64.00m),
        ("Almofada de Tinta Entintada Preta",            "LOG-029", 18.90m),
        ("Caneta Marcadora Permanente Preta (kit 10un)", "LOG-030", 34.90m),
    ];

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
    public async Task<SeedResult> EnsureSeededAsync(int initialOrders = 10_000, CancellationToken cancellationToken = default)
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
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var customers = new List<Customer>(CustomerCount);

            for (int i = 0; i < CustomerCount; i++)
            {
                var name = faker.Name.FullName();
                if (!seenNames.Add(name))
                    name = $"{name} {faker.Name.LastName()}";

                seenNames.Add(name);
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
            var products = CatalogProducts
                .Select(p => new Product(p.Name, p.Sku, p.Price))
                .ToList();

            _db.Products.AddRange(products);
            await _db.SaveChangesAsync(cancellationToken);
            productsCreated = products.Count;
            _logger.LogInformation("{Count} produtos criados (Engenharia Civil, Vestuário, Logística).", productsCreated);
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

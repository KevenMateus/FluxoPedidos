using Microsoft.EntityFrameworkCore;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core. Usado para ESCRITA (criação de pedidos) e para aplicar o
/// schema (EnsureCreated/Migrations). As leituras pesadas vão por Dapper, fora daqui.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

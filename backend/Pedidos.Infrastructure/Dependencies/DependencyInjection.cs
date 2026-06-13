using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pedidos.Application.Auth.Interfaces;
using Pedidos.Application.Catalog.Interfaces;
using Pedidos.Application.Notifications;
using Pedidos.Application.Orders.Interfaces;
using Pedidos.Application.Reports.Interfaces;
using Pedidos.Infrastructure.Dapper;
using Pedidos.Infrastructure.Notifications;
using Pedidos.Infrastructure.Persistence;
using Pedidos.Infrastructure.Repositories;
using Pedidos.Infrastructure.Security;
using Pedidos.Infrastructure.Seed;

namespace Pedidos.Infrastructure;

/// <summary>Registro de DI da camada de Infraestrutura (EF, Dapper, notificações, seed).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));

        services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IRevenueRepository, RevenueRepository>();
        services.AddScoped<ICatalogReadRepository, CatalogReadRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        var notificationsBaseUrl = configuration["Notifications:BaseUrl"] ?? "http://localhost:3001";
        services.AddHttpClient<IOrderCreatedNotifier, HttpOrderCreatedNotifier>(client =>
        {
            client.BaseAddress = new Uri(notificationsBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddScoped(sp => new DataSeeder(
            sp.GetRequiredService<AppDbContext>(),
            connectionString,
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<ILogger<DataSeeder>>()));

        return services;
    }
}

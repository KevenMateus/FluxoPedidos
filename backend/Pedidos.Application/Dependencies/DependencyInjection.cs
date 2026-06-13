using Microsoft.Extensions.DependencyInjection;
using Pedidos.Application.Auth.Interfaces;
using Pedidos.Application.Auth.Services;
using Pedidos.Application.Catalog.Interfaces;
using Pedidos.Application.Catalog.Services;
using Pedidos.Application.Orders.Interfaces;
using Pedidos.Application.Orders.Services;
using Pedidos.Application.Reports.Interfaces;
using Pedidos.Application.Reports.Services;

namespace Pedidos.Application;

/// <summary>Registro de DI dos serviços da camada de Aplicação.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}

using Pedidos.Api;
using Pedidos.Api.Dependencies;
using Pedidos.Application;
using Pedidos.Infrastructure;
using Pedidos.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pedidos API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Configuration.GetValue("Seed:Enabled", true))
{
    await SeedOnStartupAsync(app);
}

app.Run();

static async Task SeedOnStartupAsync(WebApplication app)
{
    var initialOrders = app.Configuration.GetValue("Seed:InitialOrders", 5_000);
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            var result = await seeder.EnsureSeededAsync(initialOrders);
            logger.LogInformation("Seed concluído: {@Result}", result);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Banco indisponível (tentativa {Attempt}/{Max}). Nova tentativa em 3s...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program { }

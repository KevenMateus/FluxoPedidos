namespace Pedidos.Infrastructure.Security;

/// <summary>Configuração do JWT, vinda da seção "Jwt" do appsettings/ambiente.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PedidosApi";
    public string Audience { get; set; } = "PedidosApi";
    public int ExpirationMinutes { get; set; } = 480;
}

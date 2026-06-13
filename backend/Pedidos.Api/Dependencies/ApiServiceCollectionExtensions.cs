using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pedidos.Infrastructure.Security;

namespace Pedidos.Api.Dependencies;

/// <summary>Registro de DI específico da camada de API (controllers, Swagger, JWT, CORS).</summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>Nome da política de CORS aplicada à aplicação.</summary>
    public const string CorsPolicy = "frontend";

    /// <summary>Registra os serviços da camada de API.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerWithJwt();
        services.AddJwtAuthentication(configuration);
        services.AddCorsPolicy();
        return services;
    }

    private static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Pedidos API",
                Version = "v1",
                Description = "API de Pedidos — Clean Architecture + Vertical Slices, EF Core (escrita) e Dapper (leitura pesada)."
            });

            foreach (var xml in new[] { "Pedidos.Api.xml", "Pedidos.Application.xml" })
            {
                var path = Path.Combine(AppContext.BaseDirectory, xml);
                if (File.Exists(path))
                    options.IncludeXmlComments(path);
            }

            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };
            options.AddSecurityDefinition("Bearer", scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
        });
        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        return services;
    }

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });
        return services;
    }
}

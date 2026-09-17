using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.WebHost.Security;

/// <summary>
/// Nomes de claim compartilhados por convenção entre todos os serviços (definidos
/// originalmente em Auth.Infrastructure.Security.LeoVinciClaimTypes). Duplicados aqui como
/// constantes de string simples para não criar uma referência de projeto de
/// Financeiro.Api/Relatorios.Api para Auth.Infrastructure (isso violaria o isolamento entre
/// módulos — cada Api só sabe VALIDAR o token, não emiti-lo).
/// </summary>
public static class ClaimTypesCompartilhados
{
    public const string IdUsuario = "idUsuario";
    public const string IdConta = "idConta";
}

public static class JwtBearerExtensions
{
    /// <summary>
    /// Configura validação de JWT (não emissão — isso é exclusivo de Auth.Api/Auth.Infrastructure).
    /// Todas as APIs exceto o login validam o token com a mesma chave simétrica, issuer e
    /// audience configurados em Auth.Api (Especificação Mestre, seção 11).
    /// </summary>
    public static IServiceCollection AddSharedJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = jwtSection["Key"];
        // Em ambientes de teste a configuração pode não estar disponível no momento da
        // inicialização; permitimos um fallback explícito para a chave de testes para
        // evitar que a validação quebre os testes de integração.
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Environment.GetEnvironmentVariable("JWT_TEST_KEY")
                ?? "integration-test-key-0123456789-not-for-production";
        }
        var issuer = jwtSection["Issuer"] ?? "LeoVinciFinance.Auth";
        var audience = jwtSection["Audience"] ?? "LeoVinciFinance";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
            .AddPolicy("AdminOuComerciante", policy => policy.RequireRole("Admin", "Comerciante"));

        return services;
    }
}

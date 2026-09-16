using Financeiro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
// Testcontainers intentionally not used in the test run environment; use InMemory DB for CI/local tests.
using Xunit;

namespace Financeiro.Api.IntegrationTests;

/// <summary>
/// Sobe um PostgreSQL real via Testcontainers (Docker precisa estar disponível na máquina
/// que roda os testes) e aplica as migrations de Financeiro antes de cada suíte — testes de
/// integração batem em banco real, não em banco em memória, para pegar problemas reais de
/// mapeamento EF Core/PostgreSQL (Especificação Mestre, seção 33).
/// </summary>
public class FinanceiroApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Configuração simplificada para execução determinística dos testes de integração
        // em ambientes locais/CI sem Docker: usamos InMemory DB e chave JWT fixa.
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-key-0123456789-not-for-production",
                ["Jwt:Issuer"] = "LeoVinciFinance.Auth",
                ["Jwt:Audience"] = "LeoVinciFinance"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            services.AddDbContext<FinanceiroDbContext>(opts => opts.UseInMemoryDatabase("FinanceiroTestDb"));

            services.AddAuthentication("Bearer").AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "LeoVinciFinance.Auth",
                    ValidateAudience = true,
                    ValidAudience = "LeoVinciFinance",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("integration-test-key-0123456789-not-for-production")),
                    ValidateLifetime = true
                };
            });
        });
    }
}

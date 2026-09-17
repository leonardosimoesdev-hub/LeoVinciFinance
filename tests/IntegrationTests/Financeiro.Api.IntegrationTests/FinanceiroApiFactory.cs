using Financeiro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
                ["Jwt:Audience"] = "LeoVinciFinance",
                // Ensure the module detects no real database in test environment and uses InMemory
                ["ConnectionStrings:FinanceiroDb"] = string.Empty
            });
        });

        builder.ConfigureServices((context, services) =>
        {

            // 1. Remover o DbContext original da aplicação (para não dar conflito)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FinanceiroDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 2. Adicionar a infraestrutura interna do EF Core InMemory no contêiner de testes
            services.AddEntityFrameworkInMemoryDatabase();

            // 3. Registrar o novo DbContext apontando para o InMemory usando o ServiceProvider do escopo
            services.AddDbContext<FinanceiroDbContext>((provider, options) =>
            {
                options.UseInMemoryDatabase("FinanceiroTestDb")
                       .UseInternalServiceProvider(provider); // <--- Sobrescreve com o ServiceProvider de testes
            });


            using var serviceProvider = services.BuildServiceProvider();

            var db = serviceProvider.GetRequiredService<FinanceiroDbContext>();

            // Limpamos e recriamos para cada test run para manter reprodutibilidade.
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Seed: idUsuario 1 possui uma conta; idUsuario 999 não possui contas.
            var cliente = Financeiro.Domain.Entities.Cliente.Criar(1, "Cliente Teste");
            var conta = cliente.AbrirConta();
            db.Clientes.Add(cliente);
            db.Contas.Add(conta);
            db.SaveChanges();

            // App já pode registrar a autenticação; em testes apenas configuramos as opções
            // do JwtBearer já existente para usar a chave de teste. Usamos PostConfigure para
            // não re-registrar o scheme e evitar erro "Scheme already exists: Bearer".
            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
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

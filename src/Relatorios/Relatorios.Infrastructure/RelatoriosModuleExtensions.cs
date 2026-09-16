using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using BuildingBlocks.ServiceAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Relatorios.Application.Abstractions;
using Relatorios.Application.Commands;
using Relatorios.Application.Queries;
using Relatorios.Infrastructure.Gateways;
using Relatorios.Infrastructure.Persistence;

namespace Relatorios.Infrastructure;

/// <summary>AddRelatoriosModule — composition root do módulo (seção 4).</summary>
public static class RelatoriosModuleExtensions
{
    public static IServiceCollection AddRelatoriosModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RelatoriosDb")
            ?? throw new InvalidOperationException("Connection string 'RelatoriosDb' não configurada.");

        services.AddDbContext<RelatoriosDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", RelatoriosDbContext.Schema)));

        services.AddScoped<ISaldoDiarioConsolidadoRepository, SaldoDiarioConsolidadoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICommandHandler<CriarSaldoDiarioConsolidadoCommand, Result<CriarSaldoDiarioConsolidadoResultDto>>, CriarSaldoDiarioConsolidadoCommandHandler>();
        services.AddScoped<IQueryHandler<ObterSaldoDiarioConsolidadoQuery, Result<SaldoDiarioConsolidadoDto?>>, ObterSaldoDiarioConsolidadoQueryHandler>();

        // --- Integração síncrona com Financeiro.Api (checagem de titularidade — Comerciante) ---
        // ServiceTokenProvider, cliente Refit de Auth e políticas Polly agora vêm de
        // BuildingBlocks.ServiceAuth — eliminada a duplicação com Consolidacao.Infrastructure
        // apontada nos Ajustes round 1.
        services.AddServiceAuth(configuration);

        var financeiroBaseUrl = configuration["Financeiro:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'Financeiro:BaseUrl' ausente.");

        services.AddScoped<IFinanceiroGateway, FinanceiroGateway>();
        services.AddResilientRefitClient<IFinanceiroApiClient>(configuration, financeiroBaseUrl);

        return services;
    }
}

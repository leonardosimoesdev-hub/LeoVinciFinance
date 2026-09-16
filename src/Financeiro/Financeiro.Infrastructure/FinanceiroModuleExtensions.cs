using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Financeiro.Application.Abstractions;
using Financeiro.Application.Commands;
using Financeiro.Application.Queries;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financeiro.Infrastructure;

/// <summary>
/// AddFinanceiroModule — composition root do módulo (seção 4).
///
/// Este módulo NÃO usa Kafka/MassTransit: por decisão da ADR 0011, Financeiro nunca publica
/// nem consome eventos — sua disponibilidade não pode depender de nenhum outro módulo nem de
/// infraestrutura de mensageria. Toda a lógica de consolidação (incluindo a leitura dos
/// lançamentos de Financeiro.Api) vive inteiramente em Consolidacao.BackgroundServices.
/// </summary>
public static class FinanceiroModuleExtensions
{
    public static IServiceCollection AddFinanceiroModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FinanceiroDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Ambiente de teste/integração sem container: usa InMemoryDatabase para permitir
            // execução determinística dos testes sem exigir uma instância real do PostgreSQL.
            services.AddDbContext<FinanceiroDbContext>(options =>
                options.UseInMemoryDatabase("FinanceiroInMemoryTest"));
        }
        else
        {
            services.AddDbContext<FinanceiroDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", FinanceiroDbContext.Schema)));
        }

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IClienteReadRepository, ClienteReadRepository>();
        services.AddScoped<IContaReadRepository, ContaReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICommandHandler<CriarLancamentoCommand, Result<CriarLancamentoResultDto>>, CriarLancamentoCommandHandler>();
        services.AddScoped<IQueryHandler<ObterContasPorUsuarioQuery, Result<IReadOnlyList<ContaDto>>>, ObterContasPorUsuarioQueryHandler>();
        services.AddScoped<IQueryHandler<ObterTodasContasQuery, Result<IReadOnlyList<ContaDto>>>, ObterTodasContasQueryHandler>();
        services.AddScoped<IQueryHandler<ObterLancamentosPorContaEDataQuery, Result<IReadOnlyList<LancamentoDto>>>, ObterLancamentosPorContaEDataQueryHandler>();

        return services;
    }
}

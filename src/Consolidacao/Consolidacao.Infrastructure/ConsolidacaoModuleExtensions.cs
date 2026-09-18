using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using BuildingBlocks.Common.Extensions;
using BuildingBlocks.ServiceAuth;
using Consolidacao.Application.Abstractions;
using Consolidacao.Application.Commands;
using Consolidacao.Application.Events;
using Consolidacao.Infrastructure.Gateways;
using Consolidacao.Infrastructure.Messaging;
using Consolidacao.Infrastructure.Persistence;
using MassTransit;
using MassTransit.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Consolidacao.Infrastructure;

/// <summary>AddConsolidacaoModule — composition root do módulo (seção 4).</summary>
public static class ConsolidacaoModuleExtensions
{
    public static IServiceCollection AddConsolidacaoModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ConsolidacaoDb")
            ?? throw new InvalidOperationException("Connection string 'ConsolidacaoDb' não configurada.");

        services.AddDbContext<ConsolidacaoDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ConsolidacaoDbContext.Schema)));

        services.Configure<ConsolidacaoOptions>(configuration.GetSection(ConsolidacaoOptions.SectionName));

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConsolidacaoEventPublisher, ConsolidacaoEventPublisher>();

        services.AddScoped<ICommandHandler<IniciarConsolidacaoCommand, Result<bool>>, IniciarConsolidacaoCommandHandler>();
        services.AddScoped<ICommandHandler<ProcessarIniciadoCommand, Result<bool>>, ProcessarIniciadoCommandHandler>();
        services.AddScoped<ICommandHandler<ProcessarConcluidoCommand, Result<bool>>, ProcessarConcluidoCommandHandler>();
        services.AddScoped<ICommandHandler<ProcessarComFalhasCommand, Result<bool>>, ProcessarComFalhasCommandHandler>();

        // --- Gateways HTTP resilientes (Financeiro.Api / Relatorios.Api) via BuildingBlocks.ServiceAuth ---
        services.AddServiceAuth(configuration);

        var financeiroBaseUrl = configuration["Financeiro:BaseUrl"] ?? throw new InvalidOperationException("Configuração 'Financeiro:BaseUrl' ausente.");
        var relatoriosBaseUrl = configuration["Relatorios:BaseUrl"] ?? throw new InvalidOperationException("Configuração 'Relatorios:BaseUrl' ausente.");

        services.AddScoped<IFinanceiroGateway, FinanceiroGateway>();
        services.AddScoped<IRelatoriosGateway, RelatoriosGateway>();

        services.AddResilientRefitClient<IFinanceiroApiClient>(configuration, financeiroBaseUrl);
        services.AddResilientRefitClient<IRelatoriosApiClient>(configuration, relatoriosBaseUrl);

        // --- Kafka (MassTransit rider) — 3 tópicos / 3 consumidores, um grupo cada ---
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Configuração 'Kafka:BootstrapServers' ausente.");

        // 64 partições para cada tópico, com hash Murmur3Unsafe para particionamento
        int particoes = 64;
        var particionadorGlobal = new Partitioner(particoes, new Murmur3UnsafeHashGenerator());


        services.AddMassTransit(x =>
        {
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

            x.AddRider(rider =>
            {
                rider.AddConsumer<SaldoDiarioConsolidadoIniciadoConsumer>();
                rider.AddConsumer<SaldoDiarioConsolidadoConcluidoConsumer>();
                rider.AddConsumer<SaldoDiarioConsolidadoComFalhasConsumer>();

                rider.UsingKafka((context, k) =>
                {
                    k.Host(kafkaBootstrapServers);

                    k.TopicEndpoint<string, SaldoDiarioConsolidadoIniciadoEvento>(
                        KafkaTopics.SaldoDiarioConsolidadoIniciado, KafkaTopics.GruposDeConsumidores.Iniciado, e =>
                        {
                            e.CreateIfMissing(t => { t.NumPartitions = 3; t.ReplicationFactor = 1; });
                            e.ConcurrentMessageLimit = particoes;
                            e.ConfigureConsumer<SaldoDiarioConsolidadoIniciadoConsumer>(context, c =>
                            {
                                c.ConsumerMessage<SaldoDiarioConsolidadoIniciadoEvento>(m =>
                                {
                                    m.UsePartitioner(particionadorGlobal, ctx => EventoHelper.ChaveDeParticionamento(ctx.Message.IdConta, ctx.Message.Data));
                                });
                            });
                        });

                    k.TopicEndpoint<string, SaldoDiarioConsolidadoConcluidoEvento>(
                        KafkaTopics.SaldoDiarioConsolidadoConcluido, KafkaTopics.GruposDeConsumidores.Concluido, e =>
                        {
                            e.CreateIfMissing(t => { t.NumPartitions = 3; t.ReplicationFactor = 1; });
                            e.ConcurrentMessageLimit = particoes;
                            e.ConfigureConsumer<SaldoDiarioConsolidadoConcluidoConsumer>(context, c =>
                            {
                                c.ConsumerMessage<SaldoDiarioConsolidadoConcluidoEvento>(m =>
                                {
                                    m.UsePartitioner(particionadorGlobal, ctx => EventoHelper.ChaveDeParticionamento(ctx.Message.IdConta, ctx.Message.Data));
                                });
                            });
                        });

                    k.TopicEndpoint<string, SaldoDiarioConsolidadoComFalhasEvento>(
                        KafkaTopics.SaldoDiarioConsolidadoComFalhas, KafkaTopics.GruposDeConsumidores.ComFalhas, e =>
                        {
                            e.CreateIfMissing(t => { t.NumPartitions = 3; t.ReplicationFactor = 1; });
                            e.ConcurrentMessageLimit = particoes;
                            e.ConfigureConsumer<SaldoDiarioConsolidadoComFalhasConsumer>(context, c =>
                            {
                                c.ConsumerMessage<SaldoDiarioConsolidadoComFalhasEvento>(m =>
                                {
                                    m.UsePartitioner(particionadorGlobal, ctx => EventoHelper.ChaveDeParticionamento(ctx.Message.IdConta, ctx.Message.Data));
                                });
                            });
                        });
                });
            });
        });

        return services;
    }
}

using Consolidacao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Consolidacao.Infrastructure.Persistence;

/// <summary>Schema "consolidacao" (seção 13).</summary>
public class ConsolidacaoDbContext : DbContext
{
    public const string Schema = "consolidacao";

    public DbSet<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity> SaldoDiarioConsolidadoIniciadoEventos =>
        Set<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity>();

    public DbSet<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity> SaldoDiarioConsolidadoConcluidoEventos =>
        Set<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity>();

    public DbSet<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity> SaldoDiarioConsolidadoComFalhasEventos =>
        Set<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>();

    public ConsolidacaoDbContext(DbContextOptions<ConsolidacaoDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Domínio por eventos: mapeamentos abaixo

        // Novas tabelas por evento (domínio simplificado): uma tabela por evento
        modelBuilder.Entity<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity>(builder =>
        {
            builder.ToTable("SaldoDiarioConsolidadoIniciadoEvento");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.IdConta).HasColumnName("id_conta").IsRequired();
            builder.Property(e => e.Data).HasColumnName("data").IsRequired();
            builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
            builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();

            // Unicidade por (IdConta, Data)
            builder.HasIndex(e => new { e.IdConta, e.Data }).IsUnique();
        });

        modelBuilder.Entity<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity>(builder =>
        {
            builder.ToTable("SaldoDiarioConsolidadoConcluidoEvento");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.IdConta).HasColumnName("id_conta").IsRequired();
            builder.Property(e => e.Data).HasColumnName("data").IsRequired();
            builder.Property(e => e.Saldo).HasColumnName("saldo").IsRequired();
            builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
            builder.Property(e => e.Mensagem).HasColumnName("mensagem").HasMaxLength(2000).IsRequired();
            builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();

            builder.HasIndex(e => new { e.IdConta, e.Data }).IsUnique();
        });

        modelBuilder.Entity<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>(builder =>
        {
            builder.ToTable("SaldoDiarioConsolidadoComFalhasEvento");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.IdConta).HasColumnName("id_conta").IsRequired();
            builder.Property(e => e.Data).HasColumnName("data").IsRequired();
            builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").IsRequired();
            builder.Property(e => e.Tentativas).HasColumnName("tentativas").IsRequired();
            builder.Property(e => e.Mensagem).HasColumnName("mensagem").HasMaxLength(2000).IsRequired();
            builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();

            builder.HasIndex(e => new { e.IdConta, e.Data });
        });
    }
}

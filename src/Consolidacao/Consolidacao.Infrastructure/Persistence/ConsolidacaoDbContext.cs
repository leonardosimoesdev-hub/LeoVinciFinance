using Consolidacao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Consolidacao.Infrastructure.Persistence;

/// <summary>Schema "consolidacao" (seção 13).</summary>
public class ConsolidacaoDbContext : DbContext
{
    public const string Schema = "consolidacao";

    public DbSet<SaldoDiarioConsolidadoJob> SaldoDiarioConsolidadoJobs => Set<SaldoDiarioConsolidadoJob>();

    public ConsolidacaoDbContext(DbContextOptions<ConsolidacaoDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Job é abstrato — TPH (Table-Per-Hierarchy) com discriminador "tipo_job". Hoje só
        // existe SaldoDiarioConsolidadoJob, mas o desenho já comporta outros tipos futuros
        // sem quebrar o schema (docs/architecture/architecture.md, seção 2.4).
        modelBuilder.Entity<Job>(builder =>
        {
            builder.ToTable("jobs");
            builder.HasKey(j => j.Id);
            builder.HasDiscriminator<string>("tipo_job").HasValue<SaldoDiarioConsolidadoJob>("SaldoDiarioConsolidado");

            builder.Property(j => j.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
            builder.Property(j => j.LimiteTentativas).HasColumnName("limite_tentativas").IsRequired();
            builder.Property(j => j.CriadoEm).HasColumnName("criado_em").IsRequired();

            builder.HasMany(j => j.Etapas)
                .WithOne()
                .HasForeignKey(e => e.IdJob)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(j => j.Execucoes)
                .WithOne()
                .HasForeignKey(e => e.IdJob)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(Job.Etapas))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
            builder.Metadata.FindNavigation(nameof(Job.Execucoes))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SaldoDiarioConsolidadoJob>(builder =>
        {
            builder.Property(j => j.IdConta).HasColumnName("id_conta").IsRequired();
            builder.Property(j => j.Data).HasColumnName("data").IsRequired();
            builder.Property(j => j.CorrelationId).HasColumnName("correlation_id").IsRequired();

            // Unicidade (IdConta, Data) só faz sentido para este tipo de Job — índice filtrado
            // pelo discriminador via índice composto simples (aceitável dado que, por ora, é o
            // único tipo de Job do sistema).
            builder.HasIndex(j => new { j.IdConta, j.Data }).IsUnique();
        });

        modelBuilder.Entity<Etapa>(builder =>
        {
            builder.ToTable("etapas");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.IdJob).HasColumnName("id_job").IsRequired();
            builder.Property(e => e.Evento).HasColumnName("evento").HasConversion<int>().IsRequired();
            builder.Property(e => e.EventoAnterior).HasColumnName("evento_anterior").HasConversion<int?>();
            builder.Property(e => e.Ordem).HasColumnName("ordem").IsRequired();
            builder.Property(e => e.CriadoEm).HasColumnName("criado_em").IsRequired();
            builder.HasIndex(e => e.IdJob);
        });

        modelBuilder.Entity<Execucao>(builder =>
        {
            builder.ToTable("execucoes");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.IdJob).HasColumnName("id_job").IsRequired();
            builder.Property(e => e.Evento).HasColumnName("evento").HasConversion<int>().IsRequired();
            builder.Property(e => e.Status).HasColumnName("status").HasConversion<int>().IsRequired();
            builder.Property(e => e.DataHora).HasColumnName("data_hora").IsRequired();
            builder.Property(e => e.Mensagem).HasColumnName("mensagem").HasMaxLength(2000).IsRequired();
            builder.HasIndex(e => e.IdJob);
        });
    }
}

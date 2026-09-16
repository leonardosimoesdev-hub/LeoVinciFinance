using Microsoft.EntityFrameworkCore;
using Relatorios.Domain.Entities;

namespace Relatorios.Infrastructure.Persistence;

/// <summary>Schema "relatorios" (seção 13).</summary>
public class RelatoriosDbContext : DbContext
{
    public const string Schema = "relatorios";

    public DbSet<SaldoDiarioConsolidado> SaldosDiariosConsolidados => Set<SaldoDiarioConsolidado>();

    public RelatoriosDbContext(DbContextOptions<RelatoriosDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<SaldoDiarioConsolidado>(builder =>
        {
            builder.ToTable("saldos_diarios_consolidados");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.IdConta).HasColumnName("id_conta").IsRequired();
            builder.Property(s => s.Data).HasColumnName("data").IsRequired();

            builder.Property(s => s.Saldo)
                .HasColumnName("saldo")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(s => s.CriadoEm).HasColumnName("criado_em").IsRequired();

            // Constraint única (IdConta, Data) — seção 19: "uma conta não pode possuir dois
            // saldos consolidados para a mesma data". Garante a idempotência a nível de banco,
            // independente de corrida entre réplicas da Api.
            builder.HasIndex(s => new { s.IdConta, s.Data }).IsUnique();
        });
    }
}

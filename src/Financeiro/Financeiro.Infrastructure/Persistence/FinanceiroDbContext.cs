using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Persistence;

/// <summary>Schema "financeiro" (Especificação Mestre, seção 13).</summary>
public class FinanceiroDbContext : DbContext
{
    public const string Schema = "financeiro";

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();

    public FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Cliente>(builder =>
        {
            builder.ToTable("clientes");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdUsuario).HasColumnName("id_usuario").IsRequired();
            builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
            builder.HasIndex(c => c.IdUsuario);

            builder.HasMany(c => c.Contas)
                .WithOne()
                .HasForeignKey(conta => conta.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Metadata.FindNavigation(nameof(Cliente.Contas))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Conta>(builder =>
        {
            builder.ToTable("contas");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdCliente).HasColumnName("id_cliente").IsRequired();
            builder.HasIndex(c => c.IdCliente);
        });

        modelBuilder.Entity<Lancamento>(builder =>
        {
            builder.ToTable("lancamentos");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.IdConta).HasColumnName("id_conta").IsRequired();

            // numeric(18,2) para dinheiro — nunca double/float (seção 13).
            builder.Property(l => l.Valor)
                .HasColumnName("valor")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(l => l.Data).HasColumnName("data").IsRequired();
            builder.Property(l => l.CriadoEm).HasColumnName("criado_em").IsRequired();

            // Índice de suporte à consulta usada pela Consolidação (GET .../lancamentos/{data}).
            builder.HasIndex(l => new { l.IdConta, l.Data });

            builder.HasOne<Conta>()
                .WithMany()
                .HasForeignKey(l => l.IdConta)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

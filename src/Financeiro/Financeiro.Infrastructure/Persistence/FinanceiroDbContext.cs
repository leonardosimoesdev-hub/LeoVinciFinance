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

            builder.HasData(
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    IdUsuario = 1L,
                    Nome = "Cliente Seed 1"
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IdUsuario = 2L,
                    Nome = "Cliente Seed 2"
                },
                new
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    IdUsuario = 3L,
                    Nome = "Cliente Seed 3"
                });
        });

        modelBuilder.Entity<Conta>(builder =>
        {
            builder.ToTable("contas");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdCliente).HasColumnName("id_cliente").IsRequired();
            builder.HasIndex(c => c.IdCliente);

            builder.HasData(
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111121"),
                    IdCliente = Guid.Parse("11111111-1111-1111-1111-111111111111")
                },
                new
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111122"),
                    IdCliente = Guid.Parse("11111111-1111-1111-1111-111111111111")
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222221"),
                    IdCliente = Guid.Parse("22222222-2222-2222-2222-222222222222")
                },
                new
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    IdCliente = Guid.Parse("22222222-2222-2222-2222-222222222222")
                },
                new
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333331"),
                    IdCliente = Guid.Parse("33333333-3333-3333-3333-333333333333")
                },
                new
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333332"),
                    IdCliente = Guid.Parse("33333333-3333-3333-3333-333333333333")
                });
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

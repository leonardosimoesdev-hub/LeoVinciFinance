using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Auth. Utiliza schema próprio "auth" no PostgreSQL,
/// conforme Especificação Mestre seção 13 ("cada módulo/domínio deve possuir seu próprio
/// schema").
/// </summary>
public class AuthDbContext : DbContext
{
    public const string Schema = "auth";

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Usuario>(builder =>
        {
            builder.ToTable("usuarios");

            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).ValueGeneratedOnAdd();

            builder.Property(u => u.Username)
                .HasColumnName("username")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(u => u.Username).IsUnique();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(u => u.Perfil)
                .HasColumnName("perfil")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(u => u.Ativo)
                .HasColumnName("ativo")
                .IsRequired();
        });

        modelBuilder.Entity<Usuario>().HasData(SeedUsuarios());
    }

    /// <summary>
    /// Seed de desenvolvimento. As senhas em texto puro NUNCA aparecem aqui — apenas o hash
    /// já calculado (PBKDF2). As senhas de origem ficam documentadas apenas no README
    /// (ambiente de desenvolvimento local), nunca em código versionado de produção
    /// (Especificação Mestre, seção 12).
    /// </summary>
    private static object[] SeedUsuarios()
    {
        // Hashes calculados com o mesmo algoritmo de Pbkdf2PasswordHasher (PBKDF2-HMACSHA256,
        // 100_000 iterações) para as senhas de desenvolvimento documentadas no README:
        //   admin        -> Admin@123
        //   comerciante  -> Comerciante@123
        // Nenhuma senha em texto puro fica no código-fonte — apenas o hash. As senhas de
        // origem existem somente no README (ambiente local de desenvolvimento).
        return new object[]
        {
            new
            {
                Id = 1L,
                Username = "admin",
                PasswordHash = "100000.5SkAnAwPmVaAUarB6Lx+vQ==.pgOTRv8DkUGdQFUfWcH3vtUKaawk6mmGkRCWeXspdX8=",
                Perfil = Perfil.Admin,
                Ativo = true
            },
            new
            {
                Id = 2L,
                Username = "comerciante",
                PasswordHash = "100000.fghq1WPmDIt12xNg/s+OzA==.CAQG1cM8I1CJ5zijjFDEWa6qBWyK52xA5TujsPA2DIY=",
                Perfil = Perfil.Comerciante,
                Ativo = true
            },
            new
            {
                // Conta de serviço usada por Consolidacao.BackgroundServices e por
                // Relatorios.Api (checagem de titularidade de conta) para autenticar-se
                // como Admin em chamadas internas a Financeiro.Api / Relatorios.Api.
                // Ver docs/adr/0011-isolamento-disponibilidade-lancamentos.md.
                Id = 3L,
                Username = "service-consolidacao",
                PasswordHash = "100000.MDEyMzQ1Njc4OUFCQ0RFRg==.Blv0Y798JQ+3JTdNiSHAW3Kg7q1T79/zI4wotSey6Tc=",
                Perfil = Perfil.Admin,
                Ativo = true
            }
        };
    }
}

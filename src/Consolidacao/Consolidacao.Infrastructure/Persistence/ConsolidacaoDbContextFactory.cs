using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Consolidacao.Infrastructure.Persistence;

public class ConsolidacaoDbContextFactory : IDesignTimeDbContextFactory<ConsolidacaoDbContext>
{
    public ConsolidacaoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConsolidacaoDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("CONSOLIDACAO_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=leovincifinance;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", ConsolidacaoDbContext.Schema));

        return new ConsolidacaoDbContext(optionsBuilder.Options);
    }
}

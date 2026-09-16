using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Relatorios.Infrastructure.Persistence;

public class RelatoriosDbContextFactory : IDesignTimeDbContextFactory<RelatoriosDbContext>
{
    public RelatoriosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RelatoriosDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("RELATORIOS_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=leovincifinance;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", RelatoriosDbContext.Schema));

        return new RelatoriosDbContext(optionsBuilder.Options);
    }
}

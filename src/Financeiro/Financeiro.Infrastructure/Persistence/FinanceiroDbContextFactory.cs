using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Financeiro.Infrastructure.Persistence;

public class FinanceiroDbContextFactory : IDesignTimeDbContextFactory<FinanceiroDbContext>
{
    public FinanceiroDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinanceiroDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("FINANCEIRO_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=leovincifinance;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", FinanceiroDbContext.Schema));

        return new FinanceiroDbContext(optionsBuilder.Options);
    }
}

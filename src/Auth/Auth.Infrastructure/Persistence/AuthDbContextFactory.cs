using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auth.Infrastructure.Persistence;

/// <summary>
/// Permite rodar `dotnet ef migrations add ...` diretamente neste projeto, sem precisar do
/// host da Api (útil em pipelines/CI e no dia a dia de desenvolvimento).
/// Usa uma connection string fixa de desenvolvimento (não é usada em runtime da aplicação).
/// </summary>
public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=leovincifinance;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__ef_migrations_history", AuthDbContext.Schema));

        return new AuthDbContext(optionsBuilder.Options);
    }
}

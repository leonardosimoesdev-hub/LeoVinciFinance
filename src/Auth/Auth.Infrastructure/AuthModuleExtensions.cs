using Auth.Application.Abstractions;
using Auth.Application.Commands;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using BuildingBlocks.Common.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

/// <summary>
/// Ponto único de composição do módulo Auth (Especificação Mestre, seção 4:
/// "cada módulo deve possuir seu próprio método de configuração de DI").
/// Chamado a partir de Auth.Api (composition root) e, quando necessário para testes de
/// integração, a partir do projeto de testes.
/// </summary>
public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var connectionString = configuration.GetConnectionString("AuthDb")
            ?? throw new InvalidOperationException("Connection string 'AuthDb' não configurada.");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", AuthDbContext.Schema)));

        services.AddScoped<IUsuarioReadRepository, UsuarioRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<ICommandHandler<LoginCommand, Result<LoginResultDto>>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<ValidarTokenCommand, Result<TokenValidationResultDto>>, ValidarTokenCommandHandler>();

        return services;
    }
}

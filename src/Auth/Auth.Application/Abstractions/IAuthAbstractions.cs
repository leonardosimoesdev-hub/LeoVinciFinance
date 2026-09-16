using Auth.Domain.Entities;

namespace Auth.Application.Abstractions;

/// <summary>
/// Repositório de leitura de Usuario. Implementado em Auth.Infrastructure via EF Core.
/// </summary>
public interface IUsuarioReadRepository
{
    Task<Usuario?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<Usuario?> GetByIdAsync(long id, CancellationToken cancellationToken);
}

/// <summary>
/// Abstração de hashing de senha (algoritmo concreto decidido em Infrastructure — ver ADR de
/// senhas). Application/Domain nunca conhecem o algoritmo utilizado.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string senhaPura);
    bool Verify(string senhaPura, string hash);
}

/// <summary>
/// Abstração de emissão/validação de JWT. Implementada em Infrastructure.
/// </summary>
public interface IJwtTokenService
{
    string GerarToken(Usuario usuario, Guid? idConta);
    TokenValidationResultDto ValidarToken(string token);
}

public record TokenValidationResultDto(bool Valido, long? IdUsuario, string? Username, string? Perfil, Guid? IdConta);

using Auth.Domain.Enums;
using Auth.Domain.Exceptions;
using BuildingBlocks.Common.Domain;

namespace Auth.Domain.Entities;

/// <summary>
/// Usuário do sistema. Id é `long` por decisão explícita da Especificação Mestre (seção 16),
/// diferente das demais entidades do sistema que usam Guid.
/// </summary>
public class Usuario : LongIdEntity
{
    public string Username { get; private set; } = string.Empty;

    /// <summary>
    /// Hash de senha (nunca texto puro, nunca criptografia reversível — seção 12).
    /// O algoritmo concreto (ex.: PBKDF2 via ASP.NET Core Identity, ou Argon2/BCrypt) é uma
    /// decisão de Infrastructure; Domain apenas guarda e compara o hash já calculado.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public Perfil Perfil { get; private set; }

    public bool Ativo { get; private set; }

    // Construtor exigido pelo EF Core (materialização via reflexão) — mantido privado
    // para não permitir uso indevido fora do agregado.
    private Usuario() { }

    private Usuario(long id, string username, string passwordHash, Perfil perfil, bool ativo) : base(id)
    {
        Username = username;
        PasswordHash = passwordHash;
        Perfil = perfil;
        Ativo = ativo;
    }

    /// <summary>
    /// Cria um novo usuário. O `id` é fornecido pelo chamador (Infrastructure/seed) porque,
    /// diferente das demais entidades (Guid gerado no domínio), o id `long` de Usuario
    /// normalmente é atribuído pelo banco (identity/sequence) — ver ADR 0005.
    /// </summary>
    public static Usuario Criar(long id, string username, string passwordHash, Perfil perfil)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new AuthDomainException("O username é obrigatório.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new AuthDomainException("O hash de senha é obrigatório.");

        return new Usuario(id, username.Trim(), passwordHash, perfil, ativo: true);
    }

    public void Desativar() => Ativo = false;

    public bool VerificarPerfil(Perfil perfil) => Perfil == perfil;
}

using System.Security.Cryptography;
using Auth.Application.Abstractions;

namespace Auth.Infrastructure.Security;

/// <summary>
/// Hashing de senha com PBKDF2-HMACSHA256 (algoritmo moderno, nativo do .NET,
/// sem dependência de pacote externo). Nunca reversível — Especificação Mestre, seção 12.
///
/// Formato armazenado: "{iterações}.{salt em Base64}.{hash em Base64}"
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int Iterations = 100_000;

    public string Hash(string senhaPura)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(senhaPura, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string senhaPura, string hash)
    {
        var partes = hash.Split('.', 3);
        if (partes.Length != 3)
            return false;

        if (!int.TryParse(partes[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(partes[1]);
        var chaveEsperada = Convert.FromBase64String(partes[2]);

        var chaveCalculada = Rfc2898DeriveBytes.Pbkdf2(senhaPura, salt, iterations, HashAlgorithmName.SHA256, chaveEsperada.Length);

        return CryptographicOperations.FixedTimeEquals(chaveCalculada, chaveEsperada);
    }
}

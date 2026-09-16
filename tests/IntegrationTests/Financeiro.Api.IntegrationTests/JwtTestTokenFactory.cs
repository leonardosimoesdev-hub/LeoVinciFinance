using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Financeiro.Api.IntegrationTests;

/// <summary>
/// Gera tokens JWT localmente para os testes, usando a MESMA chave configurada em
/// FinanceiroApiFactory — evita subir Auth.Api real só para autenticar os testes de
/// Financeiro.Api (que só precisa VALIDAR o token, não emiti-lo).
/// </summary>
public static class JwtTestTokenFactory
{
    private const string Key = "integration-test-key-0123456789-not-for-production";
    private const string Issuer = "LeoVinciFinance.Auth";
    private const string Audience = "LeoVinciFinance";

    public static string GerarToken(long idUsuario, string perfil)
    {
        var claims = new List<Claim>
        {
            new("idUsuario", idUsuario.ToString()),
            new(ClaimTypes.Role, perfil),
            new(JwtRegisteredClaimNames.Sub, $"usuario-teste-{idUsuario}")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

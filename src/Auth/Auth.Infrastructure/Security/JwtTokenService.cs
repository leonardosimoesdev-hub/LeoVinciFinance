using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Application.Abstractions;
using Auth.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Security;

/// <summary>
/// Nomes de claims usados em todo o sistema — compartilhados via convenção porque os
/// demais módulos (Financeiro, Relatórios) apenas leem o token, nunca dependem de
/// Auth.Infrastructure diretamente (evita acoplamento entre módulos).
/// </summary>
public static class LeoVinciClaimTypes
{
    public const string IdUsuario = "idUsuario";
    public const string IdConta = "idConta";
    public const string Perfil = ClaimTypes.Role;
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GerarToken(Usuario usuario, Guid? idConta)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Username),
            new(LeoVinciClaimTypes.IdUsuario, usuario.Id.ToString()),
            new(LeoVinciClaimTypes.Perfil, usuario.Perfil.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (idConta is not null)
            claims.Add(new Claim(LeoVinciClaimTypes.IdConta, idConta.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiracaoMinutos),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public TokenValidationResultDto ValidarToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var idUsuario = long.Parse(principal.FindFirst(LeoVinciClaimTypes.IdUsuario)!.Value);
            var username = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var perfil = principal.FindFirst(LeoVinciClaimTypes.Perfil)?.Value;
            var idContaClaim = principal.FindFirst(LeoVinciClaimTypes.IdConta)?.Value;

            return new TokenValidationResultDto(
                Valido: true,
                IdUsuario: idUsuario,
                Username: username,
                Perfil: perfil,
                IdConta: idContaClaim is not null ? Guid.Parse(idContaClaim) : null);
        }
        catch (Exception)
        {
            return new TokenValidationResultDto(false, null, null, null, null);
        }
    }
}

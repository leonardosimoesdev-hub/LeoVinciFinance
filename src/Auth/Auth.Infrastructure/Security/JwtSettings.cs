namespace Auth.Infrastructure.Security;

/// <summary>
/// Configuração de JWT (bind de "Jwt" no appsettings/variáveis de ambiente).
/// A chave (Key) NUNCA deve ficar hardcoded em produção — deve vir de variável de
/// ambiente / secret manager (Especificação Mestre, seção 12).
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LeoVinciFinance.Auth";
    public string Audience { get; set; } = "LeoVinciFinance";
    public int ExpiracaoMinutos { get; set; } = 60;
}

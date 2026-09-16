namespace BuildingBlocks.ServiceAuth;

/// <summary>
/// Configuração da conta de serviço usada em chamadas internas entre módulos
/// (Relatorios.Api -> Financeiro.Api, Consolidacao.BackgroundServices -> Financeiro.Api /
/// Relatorios.Api). Vinculada à seção "ServiceAccount" do appsettings — nenhum valor
/// hardcoded no código (Ajustes round 1: "todos os magic numbers/strings devem estar no
/// appsettings").
/// </summary>
public class ServiceAccountOptions
{
    public const string SectionName = "ServiceAccount";

    public string Username { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    /// <summary>
    /// Token JWT de longa duração pré-gerado (ver README, seção "Conta de serviço"), guardado
    /// em configuração para evitar um `POST /api/auth/login` a cada chamada. A cada uso, o
    /// provider apenas VALIDA este token via `POST /api/auth/token` — só faz login de fato
    /// se o token estiver ausente/expirado/inválido (fallback de resiliência, não o caminho
    /// principal).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Por quanto tempo o resultado da validação fica em cache antes de checar de novo.</summary>
    public int CacheMinutos { get; set; } = 30;
}

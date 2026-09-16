namespace Consolidacao.Application.Abstractions;

/// <summary>
/// Parâmetros do processo de consolidação, vinculados à seção "Consolidacao" do appsettings
/// — nenhum "magic number" (limite de tentativas, intervalos dos hosted services) hardcoded
/// no código (Ajustes round 1).
/// </summary>
public class ConsolidacaoOptions
{
    public const string SectionName = "Consolidacao";

    /// <summary>Quantas falhas consecutivas um Job pode ter antes de virar um alerta operacional definitivo.</summary>
    public int LimiteTentativas { get; set; } = 3;

    /// <summary>Intervalo entre execuções do agendador diário (seção 25), em formato TimeSpan (ex.: "1.00:00:00").</summary>
    public TimeSpan IntervaloAgendador { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Intervalo entre execuções do reprocessador de falhas (seção 29).</summary>
    public TimeSpan IntervaloRetryFalhas { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Intervalo entre execuções do detector de lacunas (seção 30).</summary>
    public TimeSpan IntervaloDeteccaoLacunas { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Data mais antiga considerada na varredura de lacunas — evita varrer o histórico
    /// inteiro do sistema a cada execução. Formato "yyyy-MM-dd".
    /// </summary>
    public DateOnly DataMinimaVarreduraLacunas { get; set; } = new(2026, 1, 1);
}

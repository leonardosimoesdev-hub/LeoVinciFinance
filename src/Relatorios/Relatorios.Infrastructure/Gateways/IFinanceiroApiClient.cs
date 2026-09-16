using Refit;

namespace Relatorios.Infrastructure.Gateways;

/// <summary>
/// Cliente Refit tipado para Financeiro.Api, usado por Relatorios.Api para verificar
/// titularidade de conta (perfil Comerciante) e pela Consolidação para obter contas e
/// lançamentos do dia. Este arquivo fica em Relatorios.Infrastructure; a Consolidação
/// declara sua própria interface equivalente (módulos não compartilham Infrastructure).
/// </summary>
public interface IFinanceiroApiClient
{
    [Get("/api/financeiro/contas/{idUsuario}")]
    Task<IReadOnlyList<ContaResponseDto>> ObterContasPorUsuarioAsync(long idUsuario, [Header("Authorization")] string authorization);
}

public record ContaResponseDto(Guid Id, Guid IdCliente);

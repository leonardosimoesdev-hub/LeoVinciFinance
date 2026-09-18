using Consolidacao.Domain.Entities;

namespace Consolidacao.Application.Abstractions;

public interface IConsolidacaoEventRepository
{
    Task AddIniciadoAsync(Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity evento, CancellationToken cancellationToken);

    Task AddConcluidoAsync(Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity evento, CancellationToken cancellationToken);

    Task AddComFalhasAsync(Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity evento, CancellationToken cancellationToken);

    Task<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity?> ObterIniciadoPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);

    Task<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity?> ObterConcluidoPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);

    Task<IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>> ObterComFalhasPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);

    Task<IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>> ObterComFalhasElegiveisParaRetryAsync(int limiteTentativas, CancellationToken cancellationToken);

    Task<IReadOnlyList<DateOnly>> ObterDatasConcluidasAsync(Guid idConta, CancellationToken cancellationToken);
}

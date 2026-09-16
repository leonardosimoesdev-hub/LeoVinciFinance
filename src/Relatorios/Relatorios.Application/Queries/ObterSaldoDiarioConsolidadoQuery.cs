using BuildingBlocks.Common.Application;
using Relatorios.Application.Abstractions;

namespace Relatorios.Application.Queries;

/// <summary>
/// GET /api/relatorios/saldo-diario-consolidado — Admin ou Comerciante (seção 24).
/// A verificação de titularidade da conta para o perfil Comerciante é feita pela Api
/// (via IFinanceiroGateway) antes/depois de chamar este handler — ver ADR 0011 e o
/// controller para a orquestração completa.
/// </summary>
public record ObterSaldoDiarioConsolidadoQuery(Guid IdConta, DateOnly Data) : IQuery<Result<SaldoDiarioConsolidadoDto?>>;

public record SaldoDiarioConsolidadoDto(Guid IdConta, DateOnly Data, decimal Saldo);

public class ObterSaldoDiarioConsolidadoQueryHandler : IQueryHandler<ObterSaldoDiarioConsolidadoQuery, Result<SaldoDiarioConsolidadoDto?>>
{
    private readonly ISaldoDiarioConsolidadoRepository _repository;

    public ObterSaldoDiarioConsolidadoQueryHandler(ISaldoDiarioConsolidadoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SaldoDiarioConsolidadoDto?>> HandleAsync(ObterSaldoDiarioConsolidadoQuery query, CancellationToken cancellationToken)
    {
        if (query.IdConta == Guid.Empty)
            return Result<SaldoDiarioConsolidadoDto?>.Failure("idConta é obrigatório.");

        var saldo = await _repository.ObterPorContaEDataAsync(query.IdConta, query.Data, cancellationToken);

        return Result<SaldoDiarioConsolidadoDto?>.Success(
            saldo is null ? null : new SaldoDiarioConsolidadoDto(saldo.IdConta, saldo.Data, saldo.Saldo));
    }
}

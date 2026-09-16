using BuildingBlocks.Common.Application;
using Financeiro.Application.Abstractions;
using Financeiro.Application.Common;

namespace Financeiro.Application.Queries;

/// <summary>GET /api/financeiro/contas/{idUsuario} — somente Admin (seção 22).</summary>
public record ObterContasPorUsuarioQuery(long IdUsuario) : IQuery<Result<IReadOnlyList<ContaDto>>>;

/// <summary>
/// GET /api/financeiro/contas — todas as contas do sistema, somente Admin. Não faz parte do
/// contrato literal da Especificação Mestre seção 22, mas é necessário para o job noturno de
/// backfill da Consolidação (seção 25 — "job idempotente que garante consistência mesmo se
/// eventos Kafka forem perdidos"). Documentado como extensão em docs/adr/0009.
/// </summary>
public record ObterTodasContasQuery() : IQuery<Result<IReadOnlyList<ContaDto>>>;

public record ContaDto(Guid Id, Guid IdCliente);

public class ObterContasPorUsuarioQueryHandler : IQueryHandler<ObterContasPorUsuarioQuery, Result<IReadOnlyList<ContaDto>>>
{
    private readonly IContaReadRepository _contaReadRepository;

    public ObterContasPorUsuarioQueryHandler(IContaReadRepository contaReadRepository)
    {
        _contaReadRepository = contaReadRepository;
    }

    public async Task<Result<IReadOnlyList<ContaDto>>> HandleAsync(ObterContasPorUsuarioQuery query, CancellationToken cancellationToken)
    {
        if (query.IdUsuario <= 0)
            return Result<IReadOnlyList<ContaDto>>.Failure(FinanceiroMensagens.IdUsuarioObrigatorio);

        var contas = await _contaReadRepository.ObterPorIdUsuarioAsync(query.IdUsuario, cancellationToken);
        var dto = contas.Select(c => new ContaDto(c.Id, c.IdCliente)).ToList();

        return Result<IReadOnlyList<ContaDto>>.Success(dto);
    }
}

public class ObterTodasContasQueryHandler : IQueryHandler<ObterTodasContasQuery, Result<IReadOnlyList<ContaDto>>>
{
    private readonly IContaReadRepository _contaReadRepository;

    public ObterTodasContasQueryHandler(IContaReadRepository contaReadRepository)
    {
        _contaReadRepository = contaReadRepository;
    }

    public async Task<Result<IReadOnlyList<ContaDto>>> HandleAsync(ObterTodasContasQuery query, CancellationToken cancellationToken)
    {
        var contas = await _contaReadRepository.ObterTodasAsync(cancellationToken);
        var dto = contas.Select(c => new ContaDto(c.Id, c.IdCliente)).ToList();
        return Result<IReadOnlyList<ContaDto>>.Success(dto);
    }
}

/// <summary>
/// GET /api/financeiro/contas/{idConta}/lancamentos/{data} — somente Admin, usado pela
/// Consolidação (seção 22).
/// </summary>
public record ObterLancamentosPorContaEDataQuery(Guid IdConta, DateOnly Data) : IQuery<Result<IReadOnlyList<LancamentoDto>>>;

public record LancamentoDto(Guid Id, Guid IdConta, decimal Valor, DateOnly Data);

public class ObterLancamentosPorContaEDataQueryHandler : IQueryHandler<ObterLancamentosPorContaEDataQuery, Result<IReadOnlyList<LancamentoDto>>>
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaReadRepository _contaReadRepository;

    public ObterLancamentosPorContaEDataQueryHandler(ILancamentoRepository lancamentoRepository, IContaReadRepository contaReadRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _contaReadRepository = contaReadRepository;
    }

    public async Task<Result<IReadOnlyList<LancamentoDto>>> HandleAsync(ObterLancamentosPorContaEDataQuery query, CancellationToken cancellationToken)
    {
        if (query.IdConta == Guid.Empty)
            return Result<IReadOnlyList<LancamentoDto>>.Failure(FinanceiroMensagens.IdContaObrigatorio);

        var conta = await _contaReadRepository.ObterPorIdAsync(query.IdConta, cancellationToken);
        if (conta is null)
            return Result<IReadOnlyList<LancamentoDto>>.Failure(FinanceiroMensagens.ContaNaoEncontrada, ResultErrorType.NotFound);

        var lancamentos = await _lancamentoRepository.ObterPorContaEDataAsync(query.IdConta, query.Data, cancellationToken);
        var dto = lancamentos.Select(l => new LancamentoDto(l.Id, l.IdConta, l.Valor, l.Data)).ToList();

        return Result<IReadOnlyList<LancamentoDto>>.Success(dto);
    }
}

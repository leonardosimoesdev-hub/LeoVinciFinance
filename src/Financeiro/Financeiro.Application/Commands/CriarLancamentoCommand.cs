using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Financeiro.Application.Abstractions;
using Financeiro.Application.Common;
using Financeiro.Domain.Entities;
using Financeiro.Domain.Exceptions;

namespace Financeiro.Application.Commands;

/// <summary>
/// POST /api/financeiro/lancamentos (Especificação Mestre, seção 22).
/// IdUsuarioAutenticado vem do JWT (Api extrai o claim e repassa) — Application nunca lê
/// HttpContext diretamente.
///
/// Este comando NÃO publica nenhum evento de integração: por decisão explícita da
/// Especificação Mestre e da ADR 0011 ("Financeiro não pode depender/publicar eventos
/// consumidos por Consolidação/Relatórios, para que sua disponibilidade nunca dependa de
/// nenhum outro módulo"), o fluxo de consolidação é inteiramente auto-suficiente dentro de
/// Consolidacao.BackgroundServices (agendador diário + detecção de lacunas), que consulta
/// Financeiro.Api de forma síncrona quando precisa, nunca o contrário.
/// </summary>
public record CriarLancamentoCommand(Guid IdConta, decimal Valor, long IdUsuarioAutenticado)
    : ICommand<Result<CriarLancamentoResultDto>>;

public record CriarLancamentoResultDto(Guid Id, Guid IdConta, decimal Valor, DateOnly Data);

public class CriarLancamentoCommandHandler : ICommandHandler<CriarLancamentoCommand, Result<CriarLancamentoResultDto>>
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaReadRepository _contaReadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarLancamentoCommandHandler(
        ILancamentoRepository lancamentoRepository,
        IContaReadRepository contaReadRepository,
        IUnitOfWork unitOfWork)
    {
        _lancamentoRepository = lancamentoRepository;
        _contaReadRepository = contaReadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CriarLancamentoResultDto>> HandleAsync(CriarLancamentoCommand command, CancellationToken cancellationToken)
    {
        if (command.IdConta == Guid.Empty)
            return Result<CriarLancamentoResultDto>.Failure(FinanceiroMensagens.IdContaObrigatorio);

        if (command.Valor == 0)
            return Result<CriarLancamentoResultDto>.Failure(FinanceiroMensagens.ValorObrigatorioNaoPodeSerZero);

        // "usuário autenticado deve possuir a conta informada" — seção 22. Válido tanto para
        // Admin quanto Comerciante: a prova não distingue exceção para Admin neste endpoint
        // (diferente das consultas administrativas), então a checagem de titularidade é
        // aplicada uniformemente. Registrado como interpretação em docs/architecture.md.
        var possuiConta = await _contaReadRepository.UsuarioPossuiContaAsync(command.IdUsuarioAutenticado, command.IdConta, cancellationToken);
        if (!possuiConta)
            return Result<CriarLancamentoResultDto>.Failure(FinanceiroMensagens.UsuarioNaoPossuiConta, ResultErrorType.Forbidden);

        Lancamento lancamento;
        try
        {
            lancamento = Lancamento.Criar(command.IdConta, command.Valor);
        }
        catch (FinanceiroDomainException ex)
        {
            return Result<CriarLancamentoResultDto>.Failure(ex.Message);
        }

        await _lancamentoRepository.AddAsync(lancamento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CriarLancamentoResultDto>.Success(
            new CriarLancamentoResultDto(lancamento.Id, lancamento.IdConta, lancamento.Valor, lancamento.Data));
    }
}

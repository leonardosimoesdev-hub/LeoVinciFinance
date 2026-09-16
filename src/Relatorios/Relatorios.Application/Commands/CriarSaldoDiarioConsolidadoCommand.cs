using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Microsoft.Extensions.Logging;
using Relatorios.Application.Abstractions;
using Relatorios.Domain.Entities;
using Relatorios.Domain.Exceptions;

namespace Relatorios.Application.Commands;

/// <summary>
/// POST /api/relatorios/saldo-diario-consolidado — somente Admin (seção 24).
/// Usado pela Consolidação. Operação idempotente: se já existir consolidação para
/// (IdConta, Data), não cria duplicidade, registra log e retorna sucesso "não criado"
/// (a Api decide o HTTP status a partir de `Criado`).
/// </summary>
public record CriarSaldoDiarioConsolidadoCommand(Guid IdConta, DateOnly Data, decimal Saldo)
    : ICommand<Result<CriarSaldoDiarioConsolidadoResultDto>>;

public record CriarSaldoDiarioConsolidadoResultDto(Guid Id, Guid IdConta, DateOnly Data, decimal Saldo, bool Criado);

public class CriarSaldoDiarioConsolidadoCommandHandler
    : ICommandHandler<CriarSaldoDiarioConsolidadoCommand, Result<CriarSaldoDiarioConsolidadoResultDto>>
{
    private readonly ISaldoDiarioConsolidadoRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CriarSaldoDiarioConsolidadoCommandHandler> _logger;

    public CriarSaldoDiarioConsolidadoCommandHandler(
        ISaldoDiarioConsolidadoRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CriarSaldoDiarioConsolidadoCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CriarSaldoDiarioConsolidadoResultDto>> HandleAsync(
        CriarSaldoDiarioConsolidadoCommand command, CancellationToken cancellationToken)
    {
        if (command.IdConta == Guid.Empty)
            return Result<CriarSaldoDiarioConsolidadoResultDto>.Failure("idConta é obrigatório.");

        if (command.Saldo == 0)
            return Result<CriarSaldoDiarioConsolidadoResultDto>.Failure("saldo é obrigatório e não pode ser zero.");

        var existente = await _repository.ObterPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);
        if (existente is not null)
        {
            // Idempotência: mensagem duplicada / reprocessamento não deve gerar
            // duplicidade nem erro — apenas log e retorno do estado já existente
            // (seção 24 e ADR 0007).
            _logger.LogInformation(
                "Consolidação já existente para IdConta={IdConta} Data={Data}; ignorando duplicidade (idempotência).",
                command.IdConta, command.Data);

            return Result<CriarSaldoDiarioConsolidadoResultDto>.Success(
                new CriarSaldoDiarioConsolidadoResultDto(existente.Id, existente.IdConta, existente.Data, existente.Saldo, Criado: false));
        }

        SaldoDiarioConsolidado saldo;
        try
        {
            saldo = SaldoDiarioConsolidado.Criar(command.IdConta, command.Data, command.Saldo);
        }
        catch (RelatoriosDomainException ex)
        {
            return Result<CriarSaldoDiarioConsolidadoResultDto>.Failure(ex.Message);
        }

        await _repository.AddAsync(saldo, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            // Corrida entre réplicas: outra instância inseriu entre a checagem e o commit.
            // Trata como sucesso idempotente em vez de propagar erro 500 ao chamador.
            _logger.LogWarning(ex,
                "Violação de unicidade ao consolidar IdConta={IdConta} Data={Data} — tratado como duplicidade concorrente (idempotência).",
                command.IdConta, command.Data);

            var jaExistente = await _repository.ObterPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);
            return Result<CriarSaldoDiarioConsolidadoResultDto>.Success(
                new CriarSaldoDiarioConsolidadoResultDto(jaExistente!.Id, jaExistente.IdConta, jaExistente.Data, jaExistente.Saldo, Criado: false));
        }

        return Result<CriarSaldoDiarioConsolidadoResultDto>.Success(
            new CriarSaldoDiarioConsolidadoResultDto(saldo.Id, saldo.IdConta, saldo.Data, saldo.Saldo, Criado: true));
    }

    /// <summary>
    /// Detecta violação do índice único (IdConta, Data) sem acoplar Application ao driver
    /// Npgsql (a checagem por texto do erro é um trade-off documentado no ADR 0007;
    /// alternativa mais robusta seria uma exceção de domínio específica lançada por
    /// Infrastructure — candidato a refino futuro).
    /// </summary>
    private static bool IsUniqueConstraintViolation(Exception ex) =>
        ex.InnerException?.Message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase) == true
        || ex.Message.Contains("duplicate key value violates unique constraint", StringComparison.OrdinalIgnoreCase);
}

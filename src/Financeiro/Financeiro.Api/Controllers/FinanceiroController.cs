using BuildingBlocks.Common.Application;
using BuildingBlocks.WebHost.Security;
using Financeiro.Api.Contracts;
using Financeiro.Application.Commands;
using Financeiro.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
[Produces("application/json")]
public class FinanceiroController : ControllerBase
{
    private readonly IQueryHandler<ObterContasPorUsuarioQuery, Result<IReadOnlyList<ContaDto>>> _obterContasHandler;
    private readonly IQueryHandler<ObterTodasContasQuery, Result<IReadOnlyList<ContaDto>>> _obterTodasContasHandler;
    private readonly IQueryHandler<ObterLancamentosPorContaEDataQuery, Result<IReadOnlyList<LancamentoDto>>> _obterLancamentosHandler;
    private readonly ICommandHandler<CriarLancamentoCommand, Result<CriarLancamentoResultDto>> _criarLancamentoHandler;

    public FinanceiroController(
        IQueryHandler<ObterContasPorUsuarioQuery, Result<IReadOnlyList<ContaDto>>> obterContasHandler,
        IQueryHandler<ObterTodasContasQuery, Result<IReadOnlyList<ContaDto>>> obterTodasContasHandler,
        IQueryHandler<ObterLancamentosPorContaEDataQuery, Result<IReadOnlyList<LancamentoDto>>> obterLancamentosHandler,
        ICommandHandler<CriarLancamentoCommand, Result<CriarLancamentoResultDto>> criarLancamentoHandler)
    {
        _obterContasHandler = obterContasHandler;
        _obterTodasContasHandler = obterTodasContasHandler;
        _obterLancamentosHandler = obterLancamentosHandler;
        _criarLancamentoHandler = criarLancamentoHandler;
    }

    /// <summary>GET /api/financeiro/contas — todas as contas do sistema, somente Admin (uso interno/backfill).</summary>
    [HttpGet("contas")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<ContaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTodasContas(CancellationToken cancellationToken)
    {
        var result = await _obterTodasContasHandler.HandleAsync(new ObterTodasContasQuery(), cancellationToken);
        var response = result.Value!.Select(c => new ContaResponse(c.Id, c.IdCliente));
        return Ok(response);
    }

    /// <summary>GET /api/financeiro/contas/{idUsuario} — somente Admin (seção 22).</summary>
    [HttpGet("contas/{idUsuario:long}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<ContaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterContasPorUsuario(long idUsuario, CancellationToken cancellationToken)
    {
        var result = await _obterContasHandler.HandleAsync(new ObterContasPorUsuarioQuery(idUsuario), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { erro = result.Error });

        var response = result.Value!.Select(c => new ContaResponse(c.Id, c.IdCliente));
        return Ok(response);
    }

    /// <summary>
    /// GET /api/financeiro/contas/{idConta}/lancamentos/{data} — somente Admin.
    /// Consumido pela Consolidação (seção 22 e 25).
    /// </summary>
    [HttpGet("contas/{idConta:guid}/lancamentos/{data}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<LancamentoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterLancamentosPorContaEData(Guid idConta, DateOnly data, CancellationToken cancellationToken)
    {
        var result = await _obterLancamentosHandler.HandleAsync(new ObterLancamentosPorContaEDataQuery(idConta, data), cancellationToken);

        if (!result.IsSuccess)
            return result.ErrorType == ResultErrorType.NotFound
                ? NotFound(new { erro = result.Error })
                : BadRequest(new { erro = result.Error });

        var response = result.Value!.Select(l => new LancamentoResponse(l.Id, l.IdConta, l.Valor, l.Data));
        return Ok(response);
    }

    /// <summary>POST /api/financeiro/lancamentos — Admin ou Comerciante (seção 22).</summary>
    [HttpPost("lancamentos")]
    [Authorize(Policy = "AdminOuComerciante")]
    [ProducesResponseType(typeof(LancamentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarLancamento([FromBody] CriarLancamentoRequest request, CancellationToken cancellationToken)
    {
        var idUsuario = User.GetIdUsuario();

        var command = new CriarLancamentoCommand(request.IdConta, request.Valor, idUsuario);
        var result = await _criarLancamentoHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ResultErrorType.Forbidden => Forbid(),
                _ => BadRequest(new { erro = result.Error })
            };
        }

        var value = result.Value!;
        var response = new LancamentoResponse(value.Id, value.IdConta, value.Valor, value.Data);
        return CreatedAtAction(nameof(ObterLancamentosPorContaEData), new { idConta = value.IdConta, data = value.Data }, response);
    }
}

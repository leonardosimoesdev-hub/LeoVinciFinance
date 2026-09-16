using BuildingBlocks.Common.Application;
using BuildingBlocks.WebHost.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Relatorios.Api.Contracts;
using Relatorios.Application.Abstractions;
using Relatorios.Application.Commands;
using Relatorios.Application.Queries;

namespace Relatorios.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize]
[Produces("application/json")]
public class RelatoriosController : ControllerBase
{
    private readonly ICommandHandler<CriarSaldoDiarioConsolidadoCommand, Result<CriarSaldoDiarioConsolidadoResultDto>> _criarHandler;
    private readonly IQueryHandler<ObterSaldoDiarioConsolidadoQuery, Result<SaldoDiarioConsolidadoDto?>> _obterHandler;
    private readonly IFinanceiroGateway _financeiroGateway;

    public RelatoriosController(
        ICommandHandler<CriarSaldoDiarioConsolidadoCommand, Result<CriarSaldoDiarioConsolidadoResultDto>> criarHandler,
        IQueryHandler<ObterSaldoDiarioConsolidadoQuery, Result<SaldoDiarioConsolidadoDto?>> obterHandler,
        IFinanceiroGateway financeiroGateway)
    {
        _criarHandler = criarHandler;
        _obterHandler = obterHandler;
        _financeiroGateway = financeiroGateway;
    }

    /// <summary>
    /// POST /api/relatorios/saldo-diario-consolidado — somente Admin (seção 24).
    /// Usado pela Consolidação. Idempotente: reenvio do mesmo (idConta, data) não duplica.
    /// </summary>
    [HttpPost("saldo-diario-consolidado")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(SaldoDiarioConsolidadoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SaldoDiarioConsolidadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarSaldoDiarioConsolidado([FromBody] CriarSaldoDiarioConsolidadoRequest request, CancellationToken cancellationToken)
    {
        var command = new CriarSaldoDiarioConsolidadoCommand(request.IdConta, request.Data, request.Saldo);
        var result = await _criarHandler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { erro = result.Error });

        var value = result.Value!;
        var response = new SaldoDiarioConsolidadoResponse(value.IdConta, value.Data, value.Saldo);

        // 201 apenas quando de fato criado agora; reenvio idempotente retorna 200 com o
        // registro já existente (seção 24/ADR 0007).
        return value.Criado
            ? CreatedAtAction(nameof(ObterSaldoDiarioConsolidado), new { idConta = value.IdConta, data = value.Data }, response)
            : Ok(response);
    }

    /// <summary>
    /// GET /api/relatorios/saldo-diario-consolidado?idConta=&amp;data= — Admin ou Comerciante
    /// (seção 24). Comerciante só pode consultar contas que possui; Admin consulta qualquer
    /// conta. Resposta cacheada por 5 min (dado imutável no dia) e sujeita a rate limiting.
    /// </summary>
    [HttpGet("saldo-diario-consolidado")]
    [Authorize(Policy = "AdminOuComerciante")]
    [EnableRateLimiting("SaldoDiarioConsolidadoPolicy")]
    [OutputCache(PolicyName = "SaldoDiarioConsolidado")]
    [ProducesResponseType(typeof(SaldoDiarioConsolidadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterSaldoDiarioConsolidado([FromQuery] Guid idConta, [FromQuery] DateOnly data, CancellationToken cancellationToken)
    {
        if (!User.EhAdmin())
        {
            var idUsuario = User.GetIdUsuario();
            var possuiConta = await _financeiroGateway.UsuarioPossuiContaAsync(idUsuario, idConta, cancellationToken);
            if (!possuiConta)
                return Forbid();
        }

        var result = await _obterHandler.HandleAsync(new ObterSaldoDiarioConsolidadoQuery(idConta, data), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { erro = result.Error });

        if (result.Value is null)
            return NotFound(new { erro = "Saldo diário consolidado não encontrado para a conta/data informadas." });

        var value = result.Value;
        return Ok(new SaldoDiarioConsolidadoResponse(value.IdConta, value.Data, value.Saldo));
    }
}

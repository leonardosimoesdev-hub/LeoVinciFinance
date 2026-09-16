using Auth.Api.Contracts;
using Auth.Application.Abstractions;
using Auth.Application.Commands;
using BuildingBlocks.Common.Application;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

/// <summary>
/// Único conjunto de endpoints públicos do sistema (não exigem token) —
/// Especificação Mestre, seção 11: "Todas as APIs, exceto o endpoint de
/// autenticação/login, devem exigir token válido."
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ICommandHandler<LoginCommand, Result<LoginResultDto>> _loginHandler;
    private readonly ICommandHandler<ValidarTokenCommand, Result<TokenValidationResultDto>> _validarTokenHandler;

    public AuthController(
        ICommandHandler<LoginCommand, Result<LoginResultDto>> loginHandler,
        ICommandHandler<ValidarTokenCommand, Result<TokenValidationResultDto>> validarTokenHandler)
    {
        _loginHandler = loginHandler;
        _validarTokenHandler = validarTokenHandler;
    }

    /// <summary>
    /// Autentica um usuário e emite o JWT contendo IdUsuario, Username e Perfil.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(new LoginCommand(request.Username, request.Senha), cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { erro = result.Error });

        var value = result.Value!;
        return Ok(new LoginResponse(value.Token, value.IdUsuario, value.Username, value.Perfil));
    }

    /// <summary>
    /// Valida um token JWT já emitido (uso interno/diagnóstico entre serviços).
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(ValidarTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidarToken([FromBody] ValidarTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _validarTokenHandler.HandleAsync(new ValidarTokenCommand(request.Token), cancellationToken);

        if (!result.IsSuccess)
            return Ok(new ValidarTokenResponse(false, null, null, null, null));

        var value = result.Value!;
        return Ok(new ValidarTokenResponse(value.Valido, value.IdUsuario, value.Username, value.Perfil, value.IdConta));
    }
}

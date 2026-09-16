using Auth.Application.Abstractions;
using BuildingBlocks.Common.Application;

namespace Auth.Application.Commands;

public record ValidarTokenCommand(string Token) : ICommand<Result<TokenValidationResultDto>>;

/// <summary>
/// POST /api/auth/token — valida um JWT já emitido (Especificação Mestre, seção 23).
/// </summary>
public class ValidarTokenCommandHandler : ICommandHandler<ValidarTokenCommand, Result<TokenValidationResultDto>>
{
    private readonly IJwtTokenService _jwtTokenService;

    public ValidarTokenCommandHandler(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public Task<Result<TokenValidationResultDto>> HandleAsync(ValidarTokenCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return Task.FromResult(Result<TokenValidationResultDto>.Failure("Token é obrigatório."));

        var resultado = _jwtTokenService.ValidarToken(command.Token);

        return Task.FromResult(resultado.Valido
            ? Result<TokenValidationResultDto>.Success(resultado)
            : Result<TokenValidationResultDto>.Failure("Token inválido ou expirado.", ResultErrorType.Validation));
    }
}

using Auth.Application.Abstractions;
using BuildingBlocks.Common.Application;

namespace Auth.Application.Commands;

public record LoginCommand(string Username, string Senha) : ICommand<Result<LoginResultDto>>;

public record LoginResultDto(string Token, long IdUsuario, string Username, string Perfil);

/// <summary>
/// Valida credenciais e emite o JWT (Especificação Mestre, seção 23).
/// </summary>
public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUsuarioReadRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUsuarioReadRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<LoginResultDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Senha))
            return Result<LoginResultDto>.Failure("Username e senha são obrigatórios.");

        var usuario = await _usuarioRepository.GetByUsernameAsync(command.Username, cancellationToken);

        // Mensagem de erro genérica propositalmente (não revelar se o usuário existe ou não).
        if (usuario is null || !usuario.Ativo || !_passwordHasher.Verify(command.Senha, usuario.PasswordHash))
            return Result<LoginResultDto>.Failure("Credenciais inválidas.", ResultErrorType.Validation);

        // IdConta não é resolvido aqui: Auth não possui visibilidade sobre o módulo Financeiro
        // (regra de dependência entre módulos). O claim "IdConta quando aplicável" fica nulo
        // no login; autorização por conta é feita no Financeiro/Relatorios a partir do
        // IdUsuario, consultando a titularidade da conta naquele módulo. Decisão registrada
        // em docs/adr/0005-jwt-autenticacao-autorizacao.md (interpretação, não requisito
        // explícito da prova).
        var token = _jwtTokenService.GerarToken(usuario, idConta: null);

        return Result<LoginResultDto>.Success(new LoginResultDto(token, usuario.Id, usuario.Username, usuario.Perfil.ToString()));
    }
}

using Refit;

namespace BuildingBlocks.ServiceAuth;

/// <summary>
/// Cliente Refit tipado para Auth.Api, usado exclusivamente para autenticar/validar a conta
/// de serviço (login e checagem de validade de token) — nunca para autenticação de usuário
/// final, que acontece via Gateway.
/// </summary>
public interface IAuthApiClient
{
    [Post("/api/auth/login")]
    Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto request);

    [Post("/api/auth/token")]
    Task<ValidarTokenResponseDto> ValidarTokenAsync([Body] ValidarTokenRequestDto request);
}

public record LoginRequestDto(string Username, string Senha);

public record LoginResponseDto(string Token);

public record ValidarTokenRequestDto(string Token);

public record ValidarTokenResponseDto(bool Valido);

namespace Auth.Api.Contracts;

public record LoginRequest(
    string Username,
    string Senha);

public record LoginResponse(string Token);

public record ValidarTokenRequest(string Token);

public record ValidarTokenResponse(bool Valido);

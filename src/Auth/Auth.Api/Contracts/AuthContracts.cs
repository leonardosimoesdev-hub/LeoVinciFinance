using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record LoginRequest(
    [property: Required] string Username,
    [property: Required] string Senha);

public record LoginResponse(string Token, long IdUsuario, string Username, string Perfil);

public record ValidarTokenRequest([property: Required] string Token);

public record ValidarTokenResponse(bool Valido, long? IdUsuario, string? Username, string? Perfil, Guid? IdConta);

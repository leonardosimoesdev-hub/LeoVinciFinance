using System.Security.Claims;

namespace BuildingBlocks.WebHost.Security;

public static class ClaimsPrincipalExtensions
{
    public static long GetIdUsuario(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst(ClaimTypesCompartilhados.IdUsuario)?.Value
            ?? throw new InvalidOperationException("Claim 'idUsuario' ausente no token.");

        return long.Parse(raw);
    }

    public static Guid? GetIdConta(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst(ClaimTypesCompartilhados.IdConta)?.Value;
        return raw is not null ? Guid.Parse(raw) : null;
    }

    public static bool EhAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
}

using BuildingBlocks.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.WebHost.Observability;

/// <summary>
/// Implementação HTTP de <see cref="ICorrelationContextAccessor"/>, lendo o valor colocado
/// em HttpContext.Items pelo middleware UseCorrelationId.
/// </summary>
public class HttpCorrelationContextAccessor : ICorrelationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CorrelationId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;
            return raw is not null && Guid.TryParse(raw, out var id) ? id : Guid.NewGuid();
        }
    }
}

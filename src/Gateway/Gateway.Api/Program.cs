using BuildingBlocks.WebHost.Observability;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog(serviceName: "Gateway.Api");

// YARP: rotas e clusters configurados em appsettings.json (seção "ReverseProxy"),
// mapeando /api/auth/**, /api/financeiro/** e /api/relatorios/** para os respectivos
// serviços internos (Especificação Mestre, seção 29 — "gateway único de entrada").
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSharedOpenTelemetry(builder.Configuration, serviceName: "Gateway.Api");
builder.Services.AddHealthChecks();

// Rate limiting global no edge (camada adicional à do Relatorios.Api — defesa em
// profundidade): protege todos os serviços de trás do gateway contra picos de tráfego.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var chave = httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        return System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(chave, _ =>
            new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.MapReverseProxy();
app.MapHealthChecks("/health");

try
{
    Log.Information("Gateway.Api iniciando...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway.Api encerrado de forma inesperada.");
}
finally
{
    Log.CloseAndFlush();
}

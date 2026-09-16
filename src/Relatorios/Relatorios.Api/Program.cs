using System.Threading.RateLimiting;
using BuildingBlocks.WebHost.Observability;
using BuildingBlocks.WebHost.Security;
using BuildingBlocks.WebHost.Swagger;
using Microsoft.AspNetCore.RateLimiting;
using Relatorios.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog(serviceName: "Relatorios.Api");

builder.Services.AddControllers();
builder.Services.AddRelatoriosModule(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithBearerAuth("LeoVinciFinance — Relatórios API");
builder.Services.AddSharedOpenTelemetry(builder.Configuration, serviceName: "Relatorios.Api");
builder.Services.AddHealthChecks();

// Cache de saída (Redis) para GET /saldo-diario-consolidado: endpoint de alta leitura,
// dado consolidado imutável no dia (Especificação Mestre, seção 30 — "cache para reduzir
// carga no banco em consultas de leitura pesada"). Duração vem de appsettings (sem magic
// numbers — Ajustes round 1).
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var outputCacheMinutos = builder.Configuration.GetValue("OutputCache:SaldoDiarioConsolidadoMinutos", 5);

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("SaldoDiarioConsolidado", policy =>
        policy.Expire(TimeSpan.FromMinutes(outputCacheMinutos)).SetVaryByQuery("idConta", "data").Tag("saldo-diario-consolidado"));
});

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisOutputCache(options => options.Configuration = redisConnection);
}

// Rate limiting (seção 30): protege o endpoint de leitura mais acessado do sistema contra
// abuso/pico de tráfego. Limite por usuário autenticado (claim idUsuario), fallback por IP
// para chamadas ainda não autenticadas. Parâmetros vindos de appsettings:RateLimiting
// (sem magic numbers — Ajustes round 1).
var rateLimitSection = builder.Configuration.GetSection("RateLimiting:SaldoDiarioConsolidado");
var permitLimit = rateLimitSection.GetValue("PermitLimit", 30);
var janelaSegundos = rateLimitSection.GetValue("JanelaSegundos", 60);
var segmentosPorJanela = rateLimitSection.GetValue("SegmentosPorJanela", 6);
var queueLimit = rateLimitSection.GetValue("QueueLimit", 0);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("SaldoDiarioConsolidadoPolicy", httpContext =>
    {
        var chave = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.GetIdUsuario().ToString()
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        return RateLimitPartition.GetSlidingWindowLimiter(chave, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(janelaSegundos),
            SegmentsPerWindow = segmentosPorJanela,
            QueueLimit = queueLimit
        });
    });
});

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();
app.UseOutputCache();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Relatorios.Api iniciando...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Relatorios.Api encerrada de forma inesperada.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

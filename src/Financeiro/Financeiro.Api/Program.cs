using BuildingBlocks.WebHost.Observability;
using BuildingBlocks.WebHost.Security;
using BuildingBlocks.WebHost.Swagger;
using Financeiro.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog(serviceName: "Financeiro.Api");

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Ensure request body property name matching is case-insensitive and compatible
        // with test payloads that use camelCase (e.g., { "idConta": ..., "valor": ... }).
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddFinanceiroModule(builder.Configuration);
builder.Services.AddSharedJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithBearerAuth("LeoVinciFinance — Financeiro API");
builder.Services.AddSharedOpenTelemetry(builder.Configuration, serviceName: "Financeiro.Api");
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Financeiro.Api iniciando...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Financeiro.Api encerrada de forma inesperada.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

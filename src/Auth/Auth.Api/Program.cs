using Auth.Infrastructure;
using BuildingBlocks.WebHost.Observability;
using BuildingBlocks.WebHost.Swagger;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSharedSerilog(serviceName: "Auth.Api");

builder.Services.AddControllers();
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddSwaggerWithBearerAuth("LeoVinciFinance — Auth API");
builder.Services.AddSharedOpenTelemetry(builder.Configuration, serviceName: "Auth.Api");

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Auth.Api iniciando...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Auth.Api encerrada de forma inesperada.");
}
finally
{
    Log.CloseAndFlush();
}

// Necessário para WebApplicationFactory<Program> em testes de integração.
public partial class Program { }

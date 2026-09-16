using System.Net.Http.Headers;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Teste de carga do endpoint de leitura mais acessado do sistema — GET
// /api/relatorios/saldo-diario-consolidado (Especificação Mestre, seção 33: "cenário de
// carga simulando picos de consulta ao saldo consolidado").
//
// Uso:
//   1. Suba o ambiente completo (docker-compose up).
//   2. Gere um token válido (POST /api/auth/login com admin/Admin@123) e exporte:
//        export LOAD_TEST_TOKEN="<token>"
//        export LOAD_TEST_ID_CONTA="<guid de uma conta com saldo consolidado existente>"
//   3. dotnet run -c Release --project tests/LoadTests/RelatoriosLoadTest

var baseUrl = Environment.GetEnvironmentVariable("LOAD_TEST_BASE_URL") ?? "http://localhost:5000";
var token = Environment.GetEnvironmentVariable("LOAD_TEST_TOKEN")
    ?? throw new InvalidOperationException("Defina a variável de ambiente LOAD_TEST_TOKEN com um JWT válido (ver instruções no topo deste arquivo).");
var idConta = Environment.GetEnvironmentVariable("LOAD_TEST_ID_CONTA")
    ?? throw new InvalidOperationException("Defina a variável de ambiente LOAD_TEST_ID_CONTA.");
var data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd");

using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

var scenario = Scenario.Create("consultar_saldo_diario_consolidado", async context =>
{
    var request = Http.CreateRequest("GET", $"/api/relatorios/saldo-diario-consolidado?idConta={idConta}&data={data}");

    var response = await Http.Send(httpClient, request);

    return response;
})
.WithLoadSimulations(
    // Ramp-up gradual seguido de carga sustentada — simula um "horário de pico" de consultas
    // ao saldo consolidado (ex.: comerciantes conferindo o fechamento do dia anterior).
    Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("tests/LoadTests/RelatoriosLoadTest/reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
    .Run();

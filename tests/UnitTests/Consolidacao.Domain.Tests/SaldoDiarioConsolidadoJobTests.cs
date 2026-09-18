using FluentAssertions;
using Xunit;
using Consolidacao.Domain.Entities;

namespace Consolidacao.Domain.Tests;

public class SaldoDiarioConsolidadoEventosTests
{
    private static readonly Guid IdContaValido = Guid.NewGuid();
    private static readonly DateOnly DataValida = new(2026, 3, 10);

    [Fact]
    public void Iniciado_Create_ShouldPopulateFields()
    {
        var correlation = Guid.NewGuid();
        var e = SaldoDiarioConsolidadoIniciadoEventoEntity.Create(IdContaValido, DataValida, correlation);

        e.IdConta.Should().Be(IdContaValido);
        e.Data.Should().Be(DataValida);
        e.CorrelationId.Should().Be(correlation);
        e.CriadoEm.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Concluido_Create_ShouldPopulateFields()
    {
        var correlation = Guid.NewGuid();
        var e = SaldoDiarioConsolidadoConcluidoEventoEntity.Create(IdContaValido, DataValida, 123.45m, correlation, "ok");

        e.IdConta.Should().Be(IdContaValido);
        e.Data.Should().Be(DataValida);
        e.Saldo.Should().Be(123.45m);
        e.CorrelationId.Should().Be(correlation);
        e.Mensagem.Should().Be("ok");
    }

    [Fact]
    public void ComFalhas_Tentativas_ShouldIncrement()
    {
        var correlation = Guid.NewGuid();
        var e = SaldoDiarioConsolidadoComFalhasEventoEntity.Create(IdContaValido, DataValida, correlation, 1, "erro");

        e.Tentativas.Should().Be(1);
        e.IncrementTentativas();
        e.Tentativas.Should().Be(2);
    }
}

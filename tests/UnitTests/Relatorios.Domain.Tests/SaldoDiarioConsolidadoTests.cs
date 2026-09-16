using FluentAssertions;
using Relatorios.Domain.Entities;
using Relatorios.Domain.Exceptions;
using Xunit;

namespace Relatorios.Domain.Tests;

public class SaldoDiarioConsolidadoTests
{
    private static readonly Guid IdContaValido = Guid.NewGuid();
    private static readonly DateOnly DataValida = new(2026, 3, 10);

    [Theory]
    [InlineData(1500.75)]
    [InlineData(-500.25)]
    public void Criar_DeveAceitarSaldoPositivoOuNegativo(decimal saldo)
    {
        var consolidado = SaldoDiarioConsolidado.Criar(IdContaValido, DataValida, saldo);

        consolidado.Saldo.Should().Be(saldo);
        consolidado.IdConta.Should().Be(IdContaValido);
        consolidado.Data.Should().Be(DataValida);
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoSaldoForZero()
    {
        var acao = () => SaldoDiarioConsolidado.Criar(IdContaValido, DataValida, 0m);

        acao.Should().Throw<RelatoriosDomainException>()
            .WithMessage("*não pode ser zero*");
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoIdContaForVazio()
    {
        var acao = () => SaldoDiarioConsolidado.Criar(Guid.Empty, DataValida, 100m);

        acao.Should().Throw<RelatoriosDomainException>();
    }

    [Fact]
    public void Criar_DeveGerarIdUnico_ParaCadaConsolidacao()
    {
        var c1 = SaldoDiarioConsolidado.Criar(IdContaValido, DataValida, 100m);
        var c2 = SaldoDiarioConsolidado.Criar(IdContaValido, DataValida, 100m);

        // Duas instâncias criadas separadamente representam entidades distintas — a
        // deduplicação de fato é responsabilidade da constraint única (IdConta, Data) no
        // banco, não do construtor do Domain (ver Relatorios.Infrastructure).
        c1.Id.Should().NotBe(c2.Id);
    }
}

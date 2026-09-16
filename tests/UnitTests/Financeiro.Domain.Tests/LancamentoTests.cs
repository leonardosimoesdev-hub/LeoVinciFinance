using Financeiro.Domain.Entities;
using Financeiro.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Financeiro.Domain.Tests;

/// <summary>
/// Cobre a regra de negócio central do sistema (Especificação Mestre, seção 17):
/// valor decimal, nunca zero, pode ser positivo (crédito) ou negativo (débito).
/// </summary>
public class LancamentoTests
{
    private static readonly Guid IdContaValido = Guid.NewGuid();

    [Theory]
    [InlineData(100.50)]
    [InlineData(0.01)]
    [InlineData(999999.99)]
    public void Criar_DeveAceitarValorPositivo_ClassificandoComoCredito(decimal valor)
    {
        var lancamento = Lancamento.Criar(IdContaValido, valor);

        lancamento.Valor.Should().Be(valor);
        lancamento.EhCredito.Should().BeTrue();
        lancamento.EhDebito.Should().BeFalse();
    }

    [Theory]
    [InlineData(-100.50)]
    [InlineData(-0.01)]
    [InlineData(-999999.99)]
    public void Criar_DeveAceitarValorNegativo_ClassificandoComoDebito(decimal valor)
    {
        var lancamento = Lancamento.Criar(IdContaValido, valor);

        lancamento.Valor.Should().Be(valor);
        lancamento.EhDebito.Should().BeTrue();
        lancamento.EhCredito.Should().BeFalse();
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoValorForZero()
    {
        var acao = () => Lancamento.Criar(IdContaValido, 0m);

        acao.Should().Throw<FinanceiroDomainException>()
            .WithMessage("*não pode ser zero*");
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoIdContaForVazio()
    {
        var acao = () => Lancamento.Criar(Guid.Empty, 100m);

        acao.Should().Throw<FinanceiroDomainException>()
            .WithMessage("*IdConta*");
    }

    [Fact]
    public void Criar_DeveUsarDataAtual_QuandoDataNaoInformada()
    {
        var antes = DateOnly.FromDateTime(DateTime.UtcNow);

        var lancamento = Lancamento.Criar(IdContaValido, 50m);

        lancamento.Data.Should().Be(antes);
    }

    [Fact]
    public void Criar_DeveUsarDataInformada_QuandoFornecida()
    {
        var data = new DateOnly(2026, 1, 15);

        var lancamento = Lancamento.Criar(IdContaValido, 50m, data);

        lancamento.Data.Should().Be(data);
    }

    [Fact]
    public void Criar_DeveGerarIdUnico_ParaCadaLancamento()
    {
        var lancamento1 = Lancamento.Criar(IdContaValido, 10m);
        var lancamento2 = Lancamento.Criar(IdContaValido, 10m);

        lancamento1.Id.Should().NotBe(lancamento2.Id);
    }
}

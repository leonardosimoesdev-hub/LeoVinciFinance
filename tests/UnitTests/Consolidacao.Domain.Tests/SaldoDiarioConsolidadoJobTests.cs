using Consolidacao.Domain.Entities;
using Consolidacao.Domain.Enums;
using Consolidacao.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Consolidacao.Domain.Tests;

public class SaldoDiarioConsolidadoJobTests
{
    private static readonly Guid IdContaValido = Guid.NewGuid();
    private static readonly DateOnly DataValida = new(2026, 3, 10);

    [Fact]
    public void Criar_DeveCriarJobValido_SemEtapasNemExecucoes()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, limiteTentativas: 3);

        job.IdConta.Should().Be(IdContaValido);
        job.Data.Should().Be(DataValida);
        job.LimiteTentativas.Should().Be(3);
        job.CorrelationId.Should().NotBe(Guid.Empty);
        job.Etapas.Should().BeEmpty();
        job.Execucoes.Should().BeEmpty();
        job.FoiConcluido().Should().BeFalse();
    }

    [Fact]
    public void Criar_DeveLancarExcecao_QuandoIdContaForVazio()
    {
        var acao = () => SaldoDiarioConsolidadoJob.Criar(Guid.Empty, DataValida, 3);

        acao.Should().Throw<ConsolidacaoDomainException>();
    }

    [Fact]
    public void Iniciar_DeveRegistrarPrimeiraEtapaEExecucao()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, 3);

        job.Iniciar();

        job.Etapas.Should().ContainSingle(e => e.Evento == EventoTipo.SaldoDiarioConsolidadoIniciado && e.EventoAnterior == null && e.Ordem == 1);
        job.Execucoes.Should().ContainSingle(e => e.Evento == EventoTipo.SaldoDiarioConsolidadoIniciado && e.Status == StatusExecucao.Sucesso);
    }

    [Fact]
    public void Iniciar_DeveLancarExcecao_QuandoJobJaFoiIniciado()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, 3);
        job.Iniciar();

        var acao = () => job.Iniciar();

        acao.Should().Throw<ConsolidacaoDomainException>();
    }

    [Fact]
    public void RegistrarConclusao_DeveMarcarJobComoConcluido()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, 3);
        job.Iniciar();

        job.RegistrarConclusao("Saldo consolidado com sucesso.");

        job.FoiConcluido().Should().BeTrue();
        job.Etapas.Should().Contain(e => e.Evento == EventoTipo.SaldoDiarioConsolidadoConcluido && e.EventoAnterior == EventoTipo.SaldoDiarioConsolidadoIniciado);
        job.Execucoes.Should().Contain(e => e.Evento == EventoTipo.SaldoDiarioConsolidadoConcluido && e.Status == StatusExecucao.Sucesso);
    }

    [Fact]
    public void RegistrarConclusao_DeveLancarExcecao_QuandoJaConcluido()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, 3);
        job.Iniciar();
        job.RegistrarConclusao("ok");

        var acao = () => job.RegistrarConclusao("de novo");

        acao.Should().Throw<ConsolidacaoDomainException>();
    }

    [Fact]
    public void RegistrarFalha_DeveIncrementarQuantidadeDeFalhas()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, limiteTentativas: 3);
        job.Iniciar();

        job.RegistrarFalha("timeout ao chamar Financeiro.Api");

        job.QuantidadeFalhas().Should().Be(1);
        job.ExcedeuLimiteTentativas().Should().BeFalse();
        job.FoiConcluido().Should().BeFalse();
    }

    [Fact]
    public void ExcedeuLimiteTentativas_DeveSerVerdadeiro_QuandoFalhasAtingemOLimite()
    {
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, limiteTentativas: 2);
        job.Iniciar();

        job.RegistrarFalha("falha 1");
        job.RegistrarFalha("falha 2");

        job.QuantidadeFalhas().Should().Be(2);
        job.ExcedeuLimiteTentativas().Should().BeTrue();
    }

    [Fact]
    public void RegistrarFalha_ApósConcluido_NaoDeveAlterarStatusDeConclusao()
    {
        // Cenário de corrida: um retry tardio falha depois que o Job já havia sido concluído
        // por outra tentativa. O Domain permite o registro (histórico de auditoria), mas
        // FoiConcluido() continua verdadeiro — quem decide se ignora a falha é o Application
        // (ProcessarComFalhasCommandHandler já concluído -> log e no-op).
        var job = SaldoDiarioConsolidadoJob.Criar(IdContaValido, DataValida, 3);
        job.Iniciar();
        job.RegistrarConclusao("ok");

        job.RegistrarFalha("falha tardia, chegou depois da conclusão");

        job.FoiConcluido().Should().BeTrue();
    }
}

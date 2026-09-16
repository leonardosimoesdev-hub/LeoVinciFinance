using Financeiro.Domain.Entities;
using Financeiro.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Financeiro.Domain.Tests;

public class ClienteTests
{
    [Fact]
    public void Criar_DeveCriarClienteValido_ComDadosCorretos()
    {
        var cliente = Cliente.Criar(idUsuario: 42L, nome: "  Comerciante Teste  ");

        cliente.IdUsuario.Should().Be(42L);
        cliente.Nome.Should().Be("Comerciante Teste");
        cliente.Contas.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_DeveLancarExcecao_QuandoIdUsuarioInvalido(long idUsuario)
    {
        var acao = () => Cliente.Criar(idUsuario, "Nome Válido");

        acao.Should().Throw<FinanceiroDomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DeveLancarExcecao_QuandoNomeInvalido(string? nome)
    {
        var acao = () => Cliente.Criar(1L, nome!);

        acao.Should().Throw<FinanceiroDomainException>();
    }

    [Fact]
    public void AbrirConta_DeveAdicionarContaAoCliente()
    {
        var cliente = Cliente.Criar(1L, "Comerciante Teste");

        var conta = cliente.AbrirConta();

        cliente.Contas.Should().ContainSingle();
        conta.IdCliente.Should().Be(cliente.Id);
    }

    [Fact]
    public void AbrirConta_DevePermitirMultiplasContas_ParaMesmoCliente()
    {
        var cliente = Cliente.Criar(1L, "Comerciante Teste");

        cliente.AbrirConta();
        cliente.AbrirConta();

        cliente.Contas.Should().HaveCount(2);
    }
}

using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Auth.Domain.Tests;

public class UsuarioTests
{
    [Fact]
    public void Criar_DeveCriarUsuarioAtivo_ComDadosValidos()
    {
        var usuario = Usuario.Criar(1L, "  admin  ", "hash-qualquer", Perfil.Admin);

        usuario.Id.Should().Be(1L);
        usuario.Username.Should().Be("admin");
        usuario.Ativo.Should().BeTrue();
        usuario.Perfil.Should().Be(Perfil.Admin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DeveLancarExcecao_QuandoUsernameInvalido(string? username)
    {
        var acao = () => Usuario.Criar(1L, username!, "hash", Perfil.Comerciante);

        acao.Should().Throw<AuthDomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_DeveLancarExcecao_QuandoPasswordHashInvalido(string? hash)
    {
        var acao = () => Usuario.Criar(1L, "username", hash!, Perfil.Comerciante);

        acao.Should().Throw<AuthDomainException>();
    }

    [Fact]
    public void Desativar_DeveMarcarUsuarioComoInativo()
    {
        var usuario = Usuario.Criar(1L, "username", "hash", Perfil.Comerciante);

        usuario.Desativar();

        usuario.Ativo.Should().BeFalse();
    }

    [Theory]
    [InlineData(Perfil.Admin, Perfil.Admin, true)]
    [InlineData(Perfil.Admin, Perfil.Comerciante, false)]
    [InlineData(Perfil.Comerciante, Perfil.Comerciante, true)]
    public void VerificarPerfil_DeveRetornarCorretamente(Perfil perfilDoUsuario, Perfil perfilVerificado, bool esperado)
    {
        var usuario = Usuario.Criar(1L, "username", "hash", perfilDoUsuario);

        usuario.VerificarPerfil(perfilVerificado).Should().Be(esperado);
    }
}

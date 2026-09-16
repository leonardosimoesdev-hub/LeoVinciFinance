namespace Financeiro.Application.Common;

/// <summary>
/// Mensagens de validação/erro do módulo Financeiro — centralizadas como constantes (Ajustes
/// round 1: "nenhuma string mágica espalhada pelo código").
/// </summary>
public static class FinanceiroMensagens
{
    public const string IdContaObrigatorio = "idConta é obrigatório.";
    public const string ValorObrigatorioNaoPodeSerZero = "valor é obrigatório e não pode ser zero.";
    public const string UsuarioNaoPossuiConta = "Usuário não possui a conta informada.";
    public const string IdUsuarioObrigatorio = "idUsuario é obrigatório.";
    public const string ContaNaoEncontrada = "Conta não encontrada.";
}

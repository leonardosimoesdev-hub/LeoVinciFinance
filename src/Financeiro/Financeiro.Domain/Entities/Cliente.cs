using BuildingBlocks.Common.Domain;
using Financeiro.Domain.Exceptions;

namespace Financeiro.Domain.Entities;

/// <summary>
/// Cliente (comerciante) do sistema, vinculado ao Usuario do módulo Auth via IdUsuario
/// (referência fraca — Financeiro não tem FK real para o schema "auth", apenas guarda o id;
/// isolamento entre módulos, ver docs/adr/0004-postgresql-schema-por-modulo.md).
/// </summary>
public class Cliente : Entity
{
    /// <summary>Id do Usuario em Auth (long, conforme Especificação Mestre seção 16).</summary>
    public long IdUsuario { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    private readonly List<Conta> _contas = new();
    public IReadOnlyCollection<Conta> Contas => _contas.AsReadOnly();

    private Cliente() { }

    private Cliente(Guid id, long idUsuario, string nome) : base(id)
    {
        IdUsuario = idUsuario;
        Nome = nome;
    }

    public static Cliente Criar(long idUsuario, string nome)
    {
        if (idUsuario <= 0)
            throw new FinanceiroDomainException("IdUsuario é obrigatório e deve ser válido.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new FinanceiroDomainException("O nome do cliente é obrigatório.");

        return new Cliente(Guid.NewGuid(), idUsuario, nome.Trim());
    }

    public Conta AbrirConta()
    {
        var conta = Conta.Criar(Id);
        _contas.Add(conta);
        return conta;
    }
}

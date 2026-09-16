using BuildingBlocks.Common.Domain;
using Financeiro.Domain.Exceptions;

namespace Financeiro.Domain.Entities;

public class Conta : Entity
{
    public Guid IdCliente { get; private set; }

    private Conta() { }

    private Conta(Guid id, Guid idCliente) : base(id)
    {
        IdCliente = idCliente;
    }

    internal static Conta Criar(Guid idCliente)
    {
        if (idCliente == Guid.Empty)
            throw new FinanceiroDomainException("IdCliente é obrigatório.");

        return new Conta(Guid.NewGuid(), idCliente);
    }
}

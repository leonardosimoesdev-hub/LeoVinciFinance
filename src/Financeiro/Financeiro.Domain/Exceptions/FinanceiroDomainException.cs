using BuildingBlocks.Common.Domain;

namespace Financeiro.Domain.Exceptions;

public class FinanceiroDomainException : DomainException
{
    public FinanceiroDomainException(string message) : base(message) { }
}

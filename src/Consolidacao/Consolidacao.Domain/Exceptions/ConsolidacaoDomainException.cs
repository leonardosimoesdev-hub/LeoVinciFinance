using BuildingBlocks.Common.Domain;

namespace Consolidacao.Domain.Exceptions;

public class ConsolidacaoDomainException : DomainException
{
    public ConsolidacaoDomainException(string message) : base(message) { }
}

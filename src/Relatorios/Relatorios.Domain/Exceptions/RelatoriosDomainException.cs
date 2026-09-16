using BuildingBlocks.Common.Domain;

namespace Relatorios.Domain.Exceptions;

public class RelatoriosDomainException : DomainException
{
    public RelatoriosDomainException(string message) : base(message) { }
}

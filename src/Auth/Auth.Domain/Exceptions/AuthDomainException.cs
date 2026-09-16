using BuildingBlocks.Common.Domain;

namespace Auth.Domain.Exceptions;

public class AuthDomainException : DomainException
{
    public AuthDomainException(string message) : base(message) { }
}

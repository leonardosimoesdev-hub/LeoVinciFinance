namespace BuildingBlocks.Common.Domain;

/// <summary>
/// Exceção lançada quando uma regra de negócio intrínseca a uma entidade/agregado é violada.
/// Usada exclusivamente pelas camadas Domain — nunca por Infrastructure/Api.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

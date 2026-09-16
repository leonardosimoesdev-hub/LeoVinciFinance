namespace BuildingBlocks.Common.Domain;

/// <summary>
/// Base para entidades de domínio identificadas por Guid.
/// Não depende de EF Core, ASP.NET ou qualquer infraestrutura — é usada pelos
/// projetos *.Domain de todos os módulos.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    protected Entity() { }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("O identificador da entidade não pode ser vazio.", nameof(id));

        Id = id;
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}

/// <summary>
/// Base para entidades identificadas por long (usado apenas em Auth.Domain, conforme
/// a Especificação Mestre seção 16: "O ID de usuário do módulo Auth será long").
/// </summary>
public abstract class LongIdEntity : IEquatable<LongIdEntity>
{
    public long Id { get; protected set; }

    protected LongIdEntity() { }

    protected LongIdEntity(long id)
    {
        Id = id;
    }

    public bool Equals(LongIdEntity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as LongIdEntity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

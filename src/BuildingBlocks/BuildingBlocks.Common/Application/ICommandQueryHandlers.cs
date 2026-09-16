namespace BuildingBlocks.Common.Application;

/// <summary>
/// Marker para Commands (operações de alteração) — Especificação Mestre, seção 21.
/// </summary>
public interface ICommand<TResult> { }

/// <summary>
/// Marker para Queries (operações de consulta) — Especificação Mestre, seção 21.
/// </summary>
public interface IQuery<TResult> { }

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Resultado padrão de casos de uso que podem falhar por regra de negócio/validação,
/// evitando o uso de exceções para controle de fluxo esperado (ex.: "conta não pertence
/// ao usuário", "consolidação já existe").
/// </summary>
public readonly struct Result<TValue>
{
    public bool IsSuccess { get; }
    public TValue? Value { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result(bool isSuccess, TValue? value, string? error, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<TValue> Success(TValue value) => new(true, value, null, ResultErrorType.None);

    public static Result<TValue> Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) =>
        new(false, default, error, errorType);
}

public enum ResultErrorType
{
    None,
    Validation,
    NotFound,
    Forbidden,
    Conflict
}

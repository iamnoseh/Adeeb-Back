using Adeeb.SharedKernel.Errors;

namespace Adeeb.SharedKernel.Results;

public class Result
{
    protected Result(bool isSuccess, Error? error, IReadOnlyDictionary<string, IReadOnlyList<Error>>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<Error>>? ValidationErrors { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
    public static Result ValidationFailure(IReadOnlyDictionary<string, IReadOnlyList<Error>> errors) =>
        new(false, Error.Validation("validation.failed", "Validation.Failed"), errors);
}

public sealed class Result<T> : Result
{
    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error) : base(false, error) { }
    private Result(IReadOnlyDictionary<string, IReadOnlyList<Error>> errors) : base(false, Error.Validation("validation.failed", "Validation.Failed"), errors) { }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);
    public new static Result<T> ValidationFailure(IReadOnlyDictionary<string, IReadOnlyList<Error>> errors) => new(errors);
}

namespace TemplateApp.Domain.Common.Results;

public static class Result
{
    public static Success Success => default;

    public static Created Created => default;

    public static Updated Updated => default;

    public static Deleted Deleted => default;

    /// <summary>Propagates the errors of one result as a failed result of another type.</summary>
    public static Result<TValue> Failure<TValue>(IReadOnlyList<Error> errors) => Result<TValue>.FromErrors(errors);
}

public sealed class Result<TValue> : IOperationResult, IErrorResultFactory<Result<TValue>>
{
    private readonly TValue? _value;
    private readonly Error[] _errors;

    private Result(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
        _errors = [];
        IsSuccess = true;
    }

    private Result(Error[] errors)
    {
        if (errors.Length == 0)
        {
            throw new ArgumentException("A failed result requires at least one error.", nameof(errors));
        }

        _errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsError => !IsSuccess;

    public IReadOnlyList<Error> Errors => _errors;

    /// <summary>Gets the value. Throws when the result is a failure, so a missed error check cannot silently yield <c>default</c>.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result. Check IsSuccess first.");

    public Error FirstError => IsError
        ? _errors[0]
        : throw new InvalidOperationException("A successful result has no errors.");

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => new([error]);

    public static implicit operator Result<TValue>(List<Error> errors) => new([.. errors]);

    public static implicit operator Result<TValue>(Error[] errors) => new([.. errors]);

    public static Result<TValue> FromErrors(IReadOnlyList<Error> errors) => new([.. errors]);

    public TNext Match<TNext>(Func<TValue, TNext> onSuccess, Func<IReadOnlyList<Error>, TNext> onError)
        => IsSuccess ? onSuccess(_value!) : onError(_errors);
}

public readonly record struct Success;

public readonly record struct Created;

public readonly record struct Updated;

public readonly record struct Deleted;

using System.Text.Json.Serialization;

namespace OSCU.Application.Common.Core;

/// <summary>
/// The outcome of an operation that can fail in an expected way.
/// </summary>
/// <remarks>
/// Exceptions are reserved for genuinely exceptional states. "That referral
/// does not exist" is an ordinary outcome of a lookup, not a fault, and using
/// exceptions for it means paying stack-unwinding costs on a routine path and
/// losing the compiler's help in remembering to handle it.
///
/// The serialised shape (<c>isSuccess</c>, <c>data</c>, <c>error</c>) is kept
/// identical to the source template so the React client contract is unchanged.
/// </remarks>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && (error is null || error == Error.None))
        {
            throw new ArgumentException("A failed result must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}

/// <inheritdoc cref="Result"/>
public class Result<T> : Result
{
    private Result(bool isSuccess, T? data, Error? error)
        : base(isSuccess, error) => Data = data;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; }

    public static Result<T> Success(T data) => new(true, data, null);

    public static new Result<T> Failure(Error error) => new(false, default, error);
}

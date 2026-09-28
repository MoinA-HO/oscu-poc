namespace OSCU.Application.Common.Core;

/// <summary>
/// The category of a failure, used by the API layer to choose an HTTP status
/// code without string-matching on <see cref="Error.Code"/>.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3
}

/// <summary>
/// An expected, non-exceptional failure.
/// </summary>
/// <remarks>
/// Carried over from the source template, with two corrections. The original
/// declared <c>namespace Template.Domain.Core</c> while physically living in
/// the Application project, so the namespace and the assembly disagreed. It
/// also had no notion of error category, which left the transport layer with
/// no way to map a failure onto a status code.
/// </remarks>
public record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);
}

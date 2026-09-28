using Microsoft.AspNetCore.Mvc;
using OSCU.Application.Common.Core;

namespace OSCU.Api.Extensions;

/// <summary>
/// Translates an application-layer <see cref="Result"/> into an HTTP response.
/// </summary>
/// <remarks>
/// This is the only place that knows about status codes, so the Application
/// layer never references ASP.NET and endpoints never repeat the mapping.
/// Failures are returned as RFC 7807 ProblemDetails, matching what the global
/// exception handler produces, so clients see one error shape throughout.
/// </remarks>
public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? TypedResults.Ok(result)
            : ToProblem(result.Error!);

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? TypedResults.NoContent()
            : ToProblem(result.Error!);

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? TypedResults.Created(locationFactory(result.Data!), result)
            : ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        string title = error.Type switch
        {
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Validation => "Bad Request",
            _ => "Internal Server Error"
        };

        return TypedResults.Problem(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = error.Description,
            // Machine-readable, so a client can branch on the code rather
            // than string-matching an English sentence that will get reworded.
            Extensions = { ["code"] = error.Code }
        });
    }
}

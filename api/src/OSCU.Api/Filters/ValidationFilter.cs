using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace OSCU.Api.Filters;

/// <summary>
/// Runs the registered FluentValidation validator for <typeparamref name="T"/>
/// before the endpoint handler sees the request.
/// </summary>
/// <remarks>
/// Attached with <c>.AddEndpointFilter&lt;ValidationFilter&lt;T&gt;&gt;()</c>,
/// which keeps validation declarative at the route definition instead of an
/// <c>if (!validation.IsValid)</c> block copied into every handler.
///
/// Carter ships its own FluentValidation hook via <c>IValidatorLocator</c>;
/// a plain endpoint filter is used here instead because it is stock ASP.NET
/// Core, is trivially unit-testable in isolation, and does not tie the
/// validation strategy to Carter's lifecycle.
/// </remarks>
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
        {
            // The filter is attached to an endpoint that does not take a T.
            // Calling next() would let unvalidated input through, so fail
            // loudly instead: this is a wiring bug, not a client error.
            return TypedResults.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = $"No argument of type '{typeof(T).Name}' was found on this endpoint."
            });
        }

        ValidationResult validation = await validator.ValidateAsync(
            argument, context.HttpContext.RequestAborted);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        return await next(context);
    }
}

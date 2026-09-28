using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OSCU.Api.Filters;

namespace OSCU.Api.UnitTests.Filters;

[TestFixture]
public class ValidationFilterTests
{
    private record Payload(string Name);

    private class PayloadValidator : AbstractValidator<Payload>
    {
        public PayloadValidator() => RuleFor(payload => payload.Name).NotEmpty();
    }

    /// <summary>
    /// A minimal EndpointFilterInvocationContext. The framework's own
    /// Create&lt;T&gt; factories are arity-specific, so a small test double is
    /// simpler than juggling overloads for one, two and three arguments.
    /// </summary>
    private class TestFilterContext(HttpContext httpContext, params object?[] arguments)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = [.. arguments];

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }

    private static EndpointFilterInvocationContext ContextFor(params object?[] arguments) =>
        new TestFilterContext(new DefaultHttpContext(), arguments);

    [Test]
    public async Task InvokeAsync_GivenAValidArgument_ShouldCallTheNextFilter()
    {
        ValidationFilter<Payload> sut = new(new PayloadValidator());
        bool nextWasCalled = false;

        object? result = await sut.InvokeAsync(
            ContextFor(new Payload("something")),
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        Assert.Multiple(() =>
        {
            Assert.That(nextWasCalled, Is.True);
            Assert.That(result, Is.Not.Null);
        });
    }

    [Test]
    public async Task InvokeAsync_GivenAnInvalidArgument_ShouldShortCircuitWith400()
    {
        ValidationFilter<Payload> sut = new(new PayloadValidator());
        bool nextWasCalled = false;

        object? result = await sut.InvokeAsync(
            ContextFor(new Payload(string.Empty)),
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>(Results.Ok());
            });

        Assert.Multiple(() =>
        {
            // The handler must never see input that failed validation.
            Assert.That(nextWasCalled, Is.False);
            Assert.That(result, Is.TypeOf<ValidationProblem>());
            Assert.That(((ValidationProblem)result!).StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        });
    }

    [Test]
    public async Task InvokeAsync_GivenAnInvalidArgument_ShouldReportEveryFailedPropertyByName()
    {
        ValidationFilter<Payload> sut = new(new PayloadValidator());

        object? result = await sut.InvokeAsync(
            ContextFor(new Payload(string.Empty)),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        ValidationProblem problem = (ValidationProblem)result!;

        Assert.That(problem.ProblemDetails.Errors, Contains.Key(nameof(Payload.Name)));
    }

    [Test]
    public async Task InvokeAsync_GivenArgumentsOfOtherTypes_ShouldStillFindTheOneItValidates()
    {
        // Route parameters arrive alongside the body, e.g. PUT /{id} taking
        // (Guid id, UpdateReferralRequest request).
        ValidationFilter<Payload> sut = new(new PayloadValidator());

        object? result = await sut.InvokeAsync(
            ContextFor(Guid.CreateVersion7(), new Payload(string.Empty), CancellationToken.None),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.That(result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public async Task InvokeAsync_GivenNoArgumentOfTheValidatedType_ShouldFailFast()
    {
        // Indicates the filter was attached to an endpoint that does not take
        // this type. Silently calling next() would let unvalidated input
        // through, so it is better to surface it loudly.
        ValidationFilter<Payload> sut = new(new PayloadValidator());

        object? result = await sut.InvokeAsync(
            ContextFor("not-a-payload"),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.That(result, Is.TypeOf<ProblemHttpResult>());
        Assert.That(
            ((ProblemHttpResult)result!).StatusCode,
            Is.EqualTo(StatusCodes.Status500InternalServerError));
    }
}

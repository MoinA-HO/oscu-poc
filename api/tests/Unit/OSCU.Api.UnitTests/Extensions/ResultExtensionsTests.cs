using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OSCU.Api.Extensions;
using OSCU.Application.Common.Core;

namespace OSCU.Api.UnitTests.Extensions;

/// <summary>
/// The single place where an application-layer failure becomes an HTTP status
/// code. Getting this wrong means a missing referral returning 500, so it is
/// worth pinning down explicitly.
/// </summary>
[TestFixture]
public class ResultExtensionsTests
{
    private const string Payload = "a-value";

    [Test]
    public void ToHttpResult_GivenSuccess_ShouldReturn200WithTheEnvelope()
    {
        IResult httpResult = Result<string>.Success(Payload).ToHttpResult();

        Ok<Result<string>> ok = (Ok<Result<string>>)httpResult;

        Assert.Multiple(() =>
        {
            Assert.That(ok.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(ok.Value!.IsSuccess, Is.True);
            Assert.That(ok.Value.Data, Is.EqualTo(Payload));
        });
    }

    [Test]
    public void ToHttpResult_GivenNonGenericSuccess_ShouldReturn204()
    {
        // A successful delete has nothing to say. 204 rather than 200 with an
        // empty envelope the client would have to ignore.
        IResult httpResult = Result.Success().ToHttpResult();

        Assert.That(httpResult, Is.TypeOf<NoContent>());
    }

    [TestCase(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [TestCase(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [TestCase(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [TestCase(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void ToHttpResult_GivenFailure_ShouldMapErrorTypeOntoStatusCode(
        ErrorType errorType,
        int expectedStatusCode)
    {
        Error error = new("Some.Code", "Something went wrong.", errorType);

        IResult httpResult = Result<string>.Failure(error).ToHttpResult();

        ProblemHttpResult problem = (ProblemHttpResult)httpResult;
        Assert.That(problem.StatusCode, Is.EqualTo(expectedStatusCode));
    }

    [Test]
    public void ToHttpResult_GivenFailure_ShouldCarryTheErrorCodeInProblemDetails()
    {
        Error error = Error.NotFound("Referral.NotFound", "No referral was found with id 'x'.");

        ProblemHttpResult problem = (ProblemHttpResult)Result<string>.Failure(error).ToHttpResult();
        ProblemDetails details = problem.ProblemDetails;

        Assert.Multiple(() =>
        {
            Assert.That(details.Detail, Is.EqualTo("No referral was found with id 'x'."));
            // Machine-readable code as an extension member, so a client can
            // branch on it without parsing prose.
            Assert.That(details.Extensions["code"], Is.EqualTo("Referral.NotFound"));
        });
    }

    [Test]
    public void ToCreatedResult_GivenSuccess_ShouldReturn201WithALocationHeader()
    {
        IResult httpResult = Result<string>.Success(Payload)
            .ToCreatedResult(value => $"/api/referrals/{value}");

        Created<Result<string>> created = (Created<Result<string>>)httpResult;

        Assert.Multiple(() =>
        {
            Assert.That(created.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
            Assert.That(created.Location, Is.EqualTo("/api/referrals/a-value"));
            Assert.That(created.Value!.Data, Is.EqualTo(Payload));
        });
    }

    [Test]
    public void ToCreatedResult_GivenFailure_ShouldNotBuildALocation()
    {
        Error error = Error.Conflict("Referral.DuplicateReference", "Already exists.");

        IResult httpResult = Result<string>.Failure(error)
            .ToCreatedResult(_ => throw new InvalidOperationException(
                "The location factory must not run for a failed result."));

        ProblemHttpResult problem = (ProblemHttpResult)httpResult;
        Assert.That(problem.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
    }
}

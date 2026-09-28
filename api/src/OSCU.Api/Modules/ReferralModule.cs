using Asp.Versioning;
using Asp.Versioning.Builder;
using Carter;
using FluentValidation;
using FluentValidation.Results;
using OSCU.Api.Extensions;
using OSCU.Api.Filters;
using OSCU.Application.Common.Core;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Api.Modules;

/// <summary>
/// The referral endpoints.
/// </summary>
/// <remarks>
/// One Carter module per feature. Adding the next of the twenty-odd tables
/// means adding a sibling module; nothing here changes and no central routing
/// file grows.
///
/// Handlers stay thin on purpose: bind, delegate to <see cref="IReferralService"/>,
/// translate the Result. Any logic that appears in this file belongs in the
/// Application layer, where it can be tested without an HTTP pipeline.
/// </remarks>
public class ReferralModule : ICarterModule
{
    private const string BasePath = "/api/referrals";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ApiVersionSet versionSet = app.NewApiVersionSet("Referrals")
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        RouteGroupBuilder referrals = app.MapGroup(BasePath)
            .WithApiVersionSet(versionSet)
            .WithTags("Referrals");

        referrals.MapGet("/", GetReferrals)
            .WithName("GetReferrals")
            .WithSummary("Lists referrals with filtering, sorting and paging.")
            .Produces<Result<PagedResult<ReferralResponse>>>()
            .ProducesValidationProblem();

        referrals.MapGet("/statuses", GetStatuses)
            .WithName("GetReferralStatuses")
            .WithSummary("Lists the permitted referral statuses.")
            .Produces<Result<IReadOnlyCollection<string>>>();

        referrals.MapGet("/{id:guid}", GetReferralById)
            .WithName("GetReferralById")
            .WithSummary("Retrieves a single referral.")
            .Produces<Result<ReferralResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        referrals.MapPost("/", CreateReferral)
            .WithName("CreateReferral")
            .WithSummary("Creates a referral.")
            .AddEndpointFilter<ValidationFilter<CreateReferralRequest>>()
            .Produces<Result<ReferralResponse>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        referrals.MapPut("/{id:guid}", UpdateReferral)
            .WithName("UpdateReferral")
            .WithSummary("Amends a referral.")
            .AddEndpointFilter<ValidationFilter<UpdateReferralRequest>>()
            .Produces<Result<ReferralResponse>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        referrals.MapDelete("/{id:guid}", DeleteReferral)
            .WithName("DeleteReferral")
            .WithSummary("Deletes a referral.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    /// <remarks>
    /// The list options are bound as discrete query parameters rather than one
    /// <c>[AsParameters]</c> object so that each appears in the OpenAPI
    /// document with its own name, type and default.
    ///
    /// <see cref="ValidationFilter{T}"/> matches on arguments, and the query
    /// object does not exist until it is assembled here, so this endpoint
    /// validates explicitly. Everything with a request body uses the filter.
    /// </remarks>
    private static async Task<IResult> GetReferrals(
        IReferralService referralService,
        IValidator<ReferralListQuery> validator,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = ReferralListQuery.DefaultPageSize,
        string? search = null,
        string? status = null,
        ReferralSortField sortBy = ReferralSortField.ReceivedDate,
        SortDirection sortDirection = SortDirection.Descending)
    {
        ReferralListQuery query = new()
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        ValidationResult validation = await validator.ValidateAsync(query, cancellationToken);

        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        Result<PagedResult<ReferralResponse>> result =
            await referralService.GetAsync(query, cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// Lets the front end populate its status filter and dropdowns from the
    /// server, so the permitted values live in exactly one place.
    /// </summary>
    private static IResult GetStatuses() =>
        Result<IReadOnlyCollection<string>>.Success([.. ReferralStatus.All]).ToHttpResult();

    private static async Task<IResult> GetReferralById(
        Guid id,
        IReferralService referralService,
        CancellationToken cancellationToken)
    {
        Result<ReferralResponse> result = await referralService.GetByIdAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateReferral(
        CreateReferralRequest request,
        IReferralService referralService,
        CancellationToken cancellationToken)
    {
        Result<ReferralResponse> result = await referralService.CreateAsync(request, cancellationToken);

        return result.ToCreatedResult(referral => $"{BasePath}/{referral.Id}");
    }

    private static async Task<IResult> UpdateReferral(
        Guid id,
        UpdateReferralRequest request,
        IReferralService referralService,
        CancellationToken cancellationToken)
    {
        Result<ReferralResponse> result = await referralService.UpdateAsync(id, request, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteReferral(
        Guid id,
        IReferralService referralService,
        CancellationToken cancellationToken)
    {
        Result result = await referralService.DeleteAsync(id, cancellationToken);

        return result.ToHttpResult();
    }
}

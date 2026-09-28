using Microsoft.Extensions.Logging;
using OSCU.Application.Common.Core;
using OSCU.Application.Common.Extensions;
using OSCU.Application.Common.Interfaces;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Application.Features.Referrals.Mapping;
using OSCU.Domain.Entities;

namespace OSCU.Application.Features.Referrals;

/// <inheritdoc cref="IReferralService"/>
public class ReferralService(
    IReferralRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<ReferralService> logger) : IReferralService
{
    public async Task<Result<PagedResult<ReferralResponse>>> GetAsync(
        ReferralListQuery query,
        CancellationToken cancellationToken = default)
    {
        PagedResult<Referral> page = await repository.GetPagedAsync(query, cancellationToken);

        logger.LogDebug(
            "Retrieved {Count} of {TotalCount} referrals (page {Page}, size {PageSize}).",
            page.Items.Count, page.TotalCount, page.Page, page.PageSize);

        return Result<PagedResult<ReferralResponse>>.Success(page.Map(referral => referral.ToResponse()));
    }

    public async Task<Result<ReferralResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        Referral? referral = await repository.GetByIdAsync(id, cancellationToken);

        if (referral is null)
        {
            logger.LogInformation("Referral {ReferralId} was requested but does not exist.", id);
            return Result<ReferralResponse>.Failure(ReferralErrors.NotFound(id));
        }

        return Result<ReferralResponse>.Success(referral.ToResponse());
    }

    public async Task<Result<ReferralResponse>> CreateAsync(
        CreateReferralRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ReferenceExistsAsync(request.ReferralReference, cancellationToken))
        {
            logger.LogInformation(
                "Rejected referral creation: reference {ReferralReference} is already in use.",
                request.ReferralReference);

            return Result<ReferralResponse>.Failure(
                ReferralErrors.DuplicateReference(request.ReferralReference));
        }

        Referral referral = Referral.Create(
            request.ReferralReference,
            request.Subject,
            request.Description,
            request.Status,
            request.ReceivedDate.ToUtc(),
            // Server-controlled: a client cannot backdate a referral.
            dateTimeProvider.UtcNow);

        repository.Add(referral);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created referral {ReferralId} with reference {ReferralReference}.",
            referral.Id, referral.ReferralReference);

        return Result<ReferralResponse>.Success(referral.ToResponse());
    }

    public async Task<Result<ReferralResponse>> UpdateAsync(
        Guid id,
        UpdateReferralRequest request,
        CancellationToken cancellationToken = default)
    {
        Referral? referral = await repository.GetByIdAsync(id, cancellationToken);

        if (referral is null)
        {
            logger.LogInformation("Referral {ReferralId} was amended but does not exist.", id);
            return Result<ReferralResponse>.Failure(ReferralErrors.NotFound(id));
        }

        referral.Update(
            request.Subject,
            request.Description,
            request.Status,
            request.ReceivedDate.ToUtc());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated referral {ReferralId}.", id);

        return Result<ReferralResponse>.Success(referral.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Referral? referral = await repository.GetByIdAsync(id, cancellationToken);

        if (referral is null)
        {
            logger.LogInformation("Referral {ReferralId} was deleted but does not exist.", id);
            return Result.Failure(ReferralErrors.NotFound(id));
        }

        repository.Remove(referral);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted referral {ReferralId}.", id);

        return Result.Success();
    }
}

using OSCU.Application.Common.Core;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals.Dtos;

namespace OSCU.Application.Features.Referrals;

/// <summary>
/// The referral use cases. This is the only surface the API layer talks to.
/// </summary>
public interface IReferralService
{
    Task<Result<PagedResult<ReferralResponse>>> GetAsync(
        ReferralListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ReferralResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ReferralResponse>> CreateAsync(
        CreateReferralRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ReferralResponse>> UpdateAsync(
        Guid id,
        UpdateReferralRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Application.Features.Referrals.Mapping;

/// <summary>
/// Entity to DTO projection, written by hand.
/// </summary>
/// <remarks>
/// No AutoMapper. For a seven-field record the reflection setup, the profile
/// registration and the configuration-validation test cost more than the six
/// lines below, and a renamed property fails at compile time here instead of
/// silently mapping to null at runtime.
/// </remarks>
public static class ReferralMappings
{
    public static ReferralResponse ToResponse(this Referral referral) => new(
        referral.Id,
        referral.ReferralReference,
        referral.Subject,
        referral.Description,
        referral.Status,
        referral.ReceivedDate,
        referral.CreatedDate);
}

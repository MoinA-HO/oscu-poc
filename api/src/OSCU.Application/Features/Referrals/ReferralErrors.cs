using OSCU.Application.Common.Core;

namespace OSCU.Application.Features.Referrals;

/// <summary>
/// Every failure this feature can produce, in one place.
/// </summary>
/// <remarks>
/// Codes are stable and machine-readable, so a client can branch on
/// <c>Referral.DuplicateReference</c> without parsing an English sentence that
/// someone will inevitably reword.
/// </remarks>
public static class ReferralErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Referral.NotFound",
        $"No referral was found with id '{id}'.");

    public static Error DuplicateReference(string referralReference) => Error.Conflict(
        "Referral.DuplicateReference",
        $"A referral with reference '{referralReference}' already exists.");
}

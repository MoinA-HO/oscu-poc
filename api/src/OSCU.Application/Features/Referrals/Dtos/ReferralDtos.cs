namespace OSCU.Application.Features.Referrals.Dtos;

/// <summary>Payload for creating a referral.</summary>
/// <remarks>
/// Deliberately not the domain entity. Exposing <see cref="Domain.Entities.Referral"/>
/// directly on the wire would let a caller set <c>Id</c> and <c>CreatedDate</c>,
/// and would couple the public API contract to internal refactors.
/// </remarks>
public record CreateReferralRequest(
    string ReferralReference,
    string Subject,
    string? Description,
    string Status,
    DateTime ReceivedDate);

/// <summary>
/// Payload for amending a referral. <c>ReferralReference</c> is absent by
/// design: it is the business identifier and is fixed at creation.
/// </summary>
public record UpdateReferralRequest(
    string Subject,
    string? Description,
    string Status,
    DateTime ReceivedDate);

/// <summary>Representation of a referral returned to clients.</summary>
public record ReferralResponse(
    Guid Id,
    string ReferralReference,
    string Subject,
    string? Description,
    string Status,
    DateTime ReceivedDate,
    DateTime CreatedDate);

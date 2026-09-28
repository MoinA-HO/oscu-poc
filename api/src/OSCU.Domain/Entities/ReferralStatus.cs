namespace OSCU.Domain.Entities;

/// <summary>
/// The controlled vocabulary for <see cref="Referral.Status"/>.
/// </summary>
/// <remarks>
/// The specification models Status as a string, so this is a set of constants
/// rather than a C# enum. That is the right call for a case management system
/// heading for 20+ tables: statuses tend to become configurable per workflow,
/// and an enum bakes them into a compiled assembly and a brittle integer
/// column. Keeping the storage type as text with a validated vocabulary lets
/// the set move to a lookup table later without a data migration.
/// </remarks>
public static class ReferralStatus
{
    public const string New = "New";
    public const string InProgress = "In Progress";
    public const string OnHold = "On Hold";
    public const string Closed = "Closed";
    public const string Rejected = "Rejected";

    /// <summary>Every recognised status, compared case-insensitively.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            New,
            InProgress,
            OnHold,
            Closed,
            Rejected
        };

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && All.Contains(status.Trim());

    /// <summary>
    /// Maps any accepted casing onto the canonical form, so the database only
    /// ever holds one spelling of each status.
    /// </summary>
    /// <exception cref="ArgumentException">The status is not recognised.</exception>
    public static string Canonicalise(string? status)
    {
        if (!IsValid(status))
        {
            throw new ArgumentException(
                $"'{status}' is not a recognised referral status. Expected one of: {string.Join(", ", All)}.",
                nameof(status));
        }

        string trimmed = status!.Trim();

        return All.First(known => string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}

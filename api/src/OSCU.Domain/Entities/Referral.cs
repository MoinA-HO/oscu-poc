namespace OSCU.Domain.Entities;

/// <summary>
/// A referral raised into the case management system.
/// </summary>
/// <remarks>
/// Setters are private and mutation happens only through <see cref="Create"/>
/// and <see cref="Update"/>, so there is no way to construct a Referral in an
/// invalid state. EF Core materialises entities via the private parameterless
/// constructor and writes backing fields directly, so private setters cost
/// nothing at the persistence layer.
/// </remarks>
public class Referral
{
    public const int ReferralReferenceMaxLength = 32;
    public const int SubjectMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int StatusMaxLength = 32;

    /// <summary>Required by EF Core. Do not use.</summary>
    private Referral()
    {
    }

    public Guid Id { get; private set; }

    public string ReferralReference { get; private set; } = null!;

    //cpublic string Forename { get; set; } = null;

    //public string Middlename { get; set; } = null;

    //public string Surname { get; set; } = null;

    public string Subject { get; private set; } = null!;

    public string? Description { get; private set; }

    public string Status { get; private set; } = null!;

    public DateTime ReceivedDate { get; private set; }

    public DateTime CreatedDate { get; private set; }

    /// <summary>
    /// Creates a valid referral, or throws.
    /// </summary>
    /// <param name="createdDate">
    /// Supplied by the caller rather than read from <c>DateTime.UtcNow</c> so
    /// that the clock is injectable and creation is deterministic under test.
    /// </param>
    public static Referral Create(
        string referralReference,
        string subject,
        string? description,
        string status,
        DateTime receivedDate,
        DateTime createdDate)
    {
        return new Referral
        {
            // Version 7 GUIDs are time-ordered. A random v4 primary key
            // scatters inserts across the B-tree and fragments the index as
            // the table grows; v7 keeps them broadly append-only.
            Id = Guid.CreateVersion7(),
            ReferralReference = NormaliseRequiredText(
                referralReference, ReferralReferenceMaxLength, nameof(referralReference)),
            Subject = NormaliseRequiredText(subject, SubjectMaxLength, nameof(subject)),
            Description = NormaliseOptionalText(description, DescriptionMaxLength, nameof(description)),
            Status = ReferralStatus.Canonicalise(status),
            ReceivedDate = RequireUtc(receivedDate, nameof(receivedDate)),
            CreatedDate = RequireUtc(createdDate, nameof(createdDate))
        };
    }

    /// <summary>
    /// Applies an amendment. Identity, reference and creation stamp are fixed
    /// at creation and are not amendable.
    /// </summary>
    public void Update(string subject, string? description, string status, DateTime receivedDate)
    {
        // Every value is validated into a local before any field is assigned,
        // so a rejected update leaves the entity exactly as it was rather than
        // half-applied.
        string validatedSubject = NormaliseRequiredText(subject, SubjectMaxLength, nameof(subject));
        string? validatedDescription = NormaliseOptionalText(
            description, DescriptionMaxLength, nameof(description));
        string validatedStatus = ReferralStatus.Canonicalise(status);
        DateTime validatedReceivedDate = RequireUtc(receivedDate, nameof(receivedDate));

        Subject = validatedSubject;
        Description = validatedDescription;
        Status = validatedStatus;
        ReceivedDate = validatedReceivedDate;
    }

    private static string NormaliseRequiredText(string value, int maxLength, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        string trimmed = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, maxLength, paramName);

        return trimmed;
    }

    private static string? NormaliseOptionalText(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Collapse "" and "   " onto NULL so absence has one representation.
            return null;
        }

        string trimmed = value.Trim();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, maxLength, paramName);

        return trimmed;
    }

    private static DateTime RequireUtc(DateTime value, string paramName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"'{paramName}' must be UTC but was {value.Kind}. Npgsql maps DateTime onto " +
                "'timestamp with time zone' and rejects any other DateTimeKind at save time; " +
                "normalise at the API with DateTimeExtensions.ToUtc().",
                paramName);
        }

        return value;
    }
}

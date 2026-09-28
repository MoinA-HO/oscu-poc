namespace OSCU.Application.Features.Referrals.Dtos;

public enum ReferralSortField
{
    ReceivedDate = 0,
    CreatedDate = 1,
    ReferralReference = 2,
    Subject = 3,
    Status = 4
}

public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}

/// <summary>
/// Filtering, sorting and paging options for the referral list.
/// </summary>
/// <remarks>
/// Sorting is expressed as an <see cref="ReferralSortField"/> enum rather than
/// a free-text column name.
///
/// The source template took the opposite approach: a string protocol
/// (<c>sort=Prop:asc&amp;filter=Prop:op:value</c>) parsed by a custom model
/// binder and executed through System.Linq.Dynamic.Core. That accepts an
/// arbitrary caller-supplied string as an expression to evaluate against the
/// data model, which is an injection surface and defeats compile-time checking
/// of column names. An enum cannot express anything the server has not already
/// agreed to, and renaming a property becomes a compiler error rather than a
/// runtime 500.
///
/// It is a record so that value equality holds, which keeps it usable as a
/// cache key and makes it straightforward to assert on in tests.
/// </remarks>
public record ReferralListQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
    public const int MaxSearchLength = 200;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>Free-text match against reference and subject.</summary>
    public string? Search { get; init; }

    /// <summary>Exact status filter. Null means "any status".</summary>
    public string? Status { get; init; }

    public ReferralSortField SortBy { get; init; } = ReferralSortField.ReceivedDate;

    public SortDirection SortDirection { get; init; } = SortDirection.Descending;
}

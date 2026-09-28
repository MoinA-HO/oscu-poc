namespace OSCU.Application.Common.Models;

/// <summary>
/// One page of results plus the metadata a client needs to render a pager.
/// </summary>
/// <remarks>
/// Paging is applied in SQL by the repository, never in memory. Materialising
/// a whole table and calling <c>Skip</c>/<c>Take</c> on the result works fine
/// against a demo dataset and falls over on a real caseload.
/// </remarks>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Empty(int page, int pageSize) => new([], page, pageSize, 0);

    /// <summary>
    /// Projects the items onto another type, preserving the paging metadata.
    /// Lets the repository deal in entities and the service return DTOs
    /// without either one re-deriving the page numbers.
    /// </summary>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector) =>
        new([.. Items.Select(selector)], Page, PageSize, TotalCount);
}

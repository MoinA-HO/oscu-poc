using Microsoft.EntityFrameworkCore;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Persistence.Repositories;

/// <inheritdoc cref="IReferralRepository"/>
public class ReferralRepository(ApplicationDbContext context) : IReferralRepository
{
    public Task<Referral?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Referrals.FirstOrDefaultAsync(referral => referral.Id == id, cancellationToken);

    public Task<bool> ReferenceExistsAsync(
        string referralReference,
        CancellationToken cancellationToken = default)
    {
        string normalised = referralReference.Trim().ToLower();

        return context.Referrals
            .AsNoTracking()
            .AnyAsync(referral => referral.ReferralReference.ToLower() == normalised, cancellationToken);
    }

    public async Task<PagedResult<Referral>> GetPagedAsync(
        ReferralListQuery query,
        CancellationToken cancellationToken = default)
    {
        // AsNoTracking: these entities are projected to DTOs and thrown away,
        // so there is nothing to gain from the change tracker snapshotting
        // every row.
        IQueryable<Referral> referrals = context.Referrals.AsNoTracking();

        referrals = ApplyFilters(referrals, query);

        // The count runs against the filtered-but-unpaged set, so the client
        // gets the true total rather than the size of the current page.
        int totalCount = await referrals.CountAsync(cancellationToken);

        List<Referral> items = await ApplySorting(referrals, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Referral>(items, query.Page, query.PageSize, totalCount);
    }

    public void Add(Referral referral) => context.Referrals.Add(referral);

    public void Remove(Referral referral) => context.Referrals.Remove(referral);

    private static IQueryable<Referral> ApplyFilters(IQueryable<Referral> referrals, ReferralListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            string status = ReferralStatus.Canonicalise(query.Status);
            referrals = referrals.Where(referral => referral.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower rather than EF.Functions.ILike so the same expression
            // translates under both Npgsql and the in-memory provider used by
            // the unit tests.
            //
            // PERFORMANCE: this cannot use the plain b-tree index. Once the
            // table is large, add a functional index —
            //   CREATE INDEX ix_referrals_subject_lower ON referrals (lower(subject));
            // — or move to a citext column or full-text search.
            string term = query.Search.Trim().ToLower();

            referrals = referrals.Where(referral =>
                referral.ReferralReference.ToLower().Contains(term) ||
                referral.Subject.ToLower().Contains(term));
        }

        return referrals;
    }

    /// <summary>
    /// Translates the sort enum into a strongly typed ordering.
    /// </summary>
    /// <remarks>
    /// A switch rather than a dynamic expression built from a caller-supplied
    /// column name. It is a few more lines, and in exchange the set of
    /// sortable columns is closed, checked by the compiler, and impossible to
    /// abuse from the query string.
    ///
    /// Every branch adds ThenBy(Id) so that ordering is total. Without a
    /// tiebreak, rows sharing a status or a date can come back in any order
    /// the planner fancies, and a row can appear on both page 1 and page 2.
    /// </remarks>
    private static IOrderedQueryable<Referral> ApplySorting(
        IQueryable<Referral> referrals,
        ReferralListQuery query)
    {
        bool ascending = query.SortDirection == SortDirection.Ascending;

        IOrderedQueryable<Referral> ordered = query.SortBy switch
        {
            ReferralSortField.ReferralReference => ascending
                ? referrals.OrderBy(referral => referral.ReferralReference)
                : referrals.OrderByDescending(referral => referral.ReferralReference),

            ReferralSortField.Subject => ascending
                ? referrals.OrderBy(referral => referral.Subject)
                : referrals.OrderByDescending(referral => referral.Subject),

            ReferralSortField.Status => ascending
                ? referrals.OrderBy(referral => referral.Status)
                : referrals.OrderByDescending(referral => referral.Status),

            ReferralSortField.CreatedDate => ascending
                ? referrals.OrderBy(referral => referral.CreatedDate)
                : referrals.OrderByDescending(referral => referral.CreatedDate),

            _ => ascending
                ? referrals.OrderBy(referral => referral.ReceivedDate)
                : referrals.OrderByDescending(referral => referral.ReceivedDate)
        };

        return ordered.ThenBy(referral => referral.Id);
    }
}

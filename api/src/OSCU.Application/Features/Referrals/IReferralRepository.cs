using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Application.Features.Referrals;

/// <summary>
/// Persistence operations for the Referral aggregate.
/// </summary>
/// <remarks>
/// The interface lives in the Application layer and is implemented in
/// Persistence, so the dependency points inwards and the service can be unit
/// tested against a mock with no database in sight.
///
/// It returns entities rather than <c>IQueryable</c>. Handing an
/// <c>IQueryable</c> to the caller would let query composition — and therefore
/// the shape of the SQL — leak out across the layer boundary, which is exactly
/// what the repository is supposed to prevent. Filtering, sorting and paging
/// are therefore parameters of <see cref="GetPagedAsync"/> and are translated
/// to SQL inside the implementation.
///
/// <c>Add</c> and <c>Remove</c> are synchronous and do not persist: they stage
/// the change on the tracked graph. Committing is <see cref="Common.Interfaces.IUnitOfWork"/>'s job.
/// </remarks>
public interface IReferralRepository
{
    Task<Referral?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Referral>> GetPagedAsync(
        ReferralListQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a referral already uses this business reference. The unique
    /// index on the column is the real guarantee; this exists so the common
    /// case returns a clean 409 rather than a database constraint violation.
    /// </summary>
    Task<bool> ReferenceExistsAsync(string referralReference, CancellationToken cancellationToken = default);

    void Add(Referral referral);

    void Remove(Referral referral);
}

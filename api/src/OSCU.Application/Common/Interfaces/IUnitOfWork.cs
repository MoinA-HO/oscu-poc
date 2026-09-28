namespace OSCU.Application.Common.Interfaces;

/// <summary>
/// Commits the work tracked across one or more repositories as a single
/// transaction.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>IReferralRepository</c>. If each repository
/// owned its own <c>SaveChangesAsync</c>, then the first operation that spans
/// two tables — and with 20+ tables planned, there will be one — would commit
/// in two transactions and leave partial writes behind on failure.
/// </remarks>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using OSCU.Application.Common.Interfaces;
using OSCU.Domain.Entities;

namespace OSCU.Persistence;

/// <summary>
/// The EF Core context, which doubles as the unit of work.
/// </summary>
/// <remarks>
/// <see cref="DbContext"/> already is a unit of work — it tracks a graph of
/// changes and commits them in one transaction — so implementing
/// <see cref="IUnitOfWork"/> here exposes that to the Application layer
/// without a wrapper class that would only forward one method.
///
/// Entity configuration lives in separate IEntityTypeConfiguration classes
/// and is picked up by assembly scan, so this file does not grow a hundred
/// lines of fluent API per table as the schema expands to 20+ entities.
/// </remarks>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Referral> Referrals => Set<Referral>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}

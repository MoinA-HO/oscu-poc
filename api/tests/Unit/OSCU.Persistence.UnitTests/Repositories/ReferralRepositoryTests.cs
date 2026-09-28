using Microsoft.EntityFrameworkCore;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;
using OSCU.Persistence.Repositories;

namespace OSCU.Persistence.UnitTests.Repositories;

/// <summary>
/// Exercises query composition — filtering, sorting, paging — against the EF
/// in-memory provider.
/// </summary>
/// <remarks>
/// The in-memory provider does NOT enforce relational constraints. The unique
/// index on ReferralReference is therefore verified in the integration suite
/// against real Postgres, not here; a test asserting it in memory would pass
/// for the wrong reason.
/// </remarks>
[TestFixture]
public class ReferralRepositoryTests
{
    private static readonly DateTime CreatedUtc = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    private ApplicationDbContext _context = null!;
    private ReferralRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            // A distinct store per test keeps them independent and parallelisable.
            .UseInMemoryDatabase($"referrals-{Guid.CreateVersion7()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new ReferralRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private async Task<Referral> GivenAReferral(
        string reference = "REF-0001",
        string subject = "A subject",
        string status = ReferralStatus.New,
        int receivedDaysAgo = 1)
    {
        Referral referral = Referral.Create(
            reference,
            subject,
            "A description",
            status,
            CreatedUtc.AddDays(-receivedDaysAgo),
            CreatedUtc);

        _sut.Add(referral);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return referral;
    }

    // ------------------------------------------------------------- GetById

    [Test]
    public async Task GetByIdAsync_GivenAnExistingId_ShouldReturnTheReferral()
    {
        Referral referral = await GivenAReferral();

        Referral? found = await _sut.GetByIdAsync(referral.Id);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.Not.Null);
            Assert.That(found!.ReferralReference, Is.EqualTo("REF-0001"));
        });
    }

    [Test]
    public async Task GetByIdAsync_GivenAnUnknownId_ShouldReturnNull()
    {
        Referral? found = await _sut.GetByIdAsync(Guid.CreateVersion7());

        Assert.That(found, Is.Null);
    }

    // ----------------------------------------------------- ReferenceExists

    [Test]
    public async Task ReferenceExistsAsync_GivenAUsedReference_ShouldReturnTrue()
    {
        await GivenAReferral(reference: "REF-0001");

        Assert.That(await _sut.ReferenceExistsAsync("REF-0001"), Is.True);
    }

    [Test]
    public async Task ReferenceExistsAsync_GivenAnUnusedReference_ShouldReturnFalse()
    {
        await GivenAReferral(reference: "REF-0001");

        Assert.That(await _sut.ReferenceExistsAsync("REF-9999"), Is.False);
    }

    [Test]
    public async Task ReferenceExistsAsync_ShouldIgnoreCaseAndSurroundingWhitespace()
    {
        await GivenAReferral(reference: "REF-0001");

        Assert.That(await _sut.ReferenceExistsAsync("  ref-0001  "), Is.True);
    }

    // ------------------------------------------------------------ GetPaged

    [Test]
    public async Task GetPagedAsync_ShouldReturnTheRequestedPageAndTheUnpagedTotal()
    {
        for (int i = 1; i <= 25; i++)
        {
            await GivenAReferral(reference: $"REF-{i:D4}", receivedDaysAgo: i);
        }

        PagedResult<Referral> page = await _sut.GetPagedAsync(new ReferralListQuery { Page = 2, PageSize = 10 });

        Assert.Multiple(() =>
        {
            Assert.That(page.Items, Has.Count.EqualTo(10));
            // TotalCount counts the filtered set, not the page.
            Assert.That(page.TotalCount, Is.EqualTo(25));
            Assert.That(page.Page, Is.EqualTo(2));
            Assert.That(page.TotalPages, Is.EqualTo(3));
            Assert.That(page.HasNextPage, Is.True);
            Assert.That(page.HasPreviousPage, Is.True);
        });
    }

    [Test]
    public async Task GetPagedAsync_GivenAPageBeyondTheEnd_ShouldReturnAnEmptyPageNotAnError()
    {
        await GivenAReferral();

        PagedResult<Referral> page = await _sut.GetPagedAsync(new ReferralListQuery { Page = 99, PageSize = 10 });

        Assert.Multiple(() =>
        {
            Assert.That(page.Items, Is.Empty);
            Assert.That(page.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetPagedAsync_ByDefault_ShouldSortByReceivedDateNewestFirst()
    {
        await GivenAReferral(reference: "REF-0001", receivedDaysAgo: 10);
        await GivenAReferral(reference: "REF-0002", receivedDaysAgo: 1);
        await GivenAReferral(reference: "REF-0003", receivedDaysAgo: 5);

        PagedResult<Referral> page = await _sut.GetPagedAsync(new ReferralListQuery());

        Assert.That(
            page.Items.Select(r => r.ReferralReference),
            Is.EqualTo(new[] { "REF-0002", "REF-0003", "REF-0001" }).AsCollection);
    }

    [Test]
    public async Task GetPagedAsync_GivenAscendingSubjectSort_ShouldOrderAccordingly()
    {
        await GivenAReferral(reference: "REF-0001", subject: "Charlie");
        await GivenAReferral(reference: "REF-0002", subject: "Alpha");
        await GivenAReferral(reference: "REF-0003", subject: "Bravo");

        PagedResult<Referral> page = await _sut.GetPagedAsync(new ReferralListQuery
        {
            SortBy = ReferralSortField.Subject,
            SortDirection = SortDirection.Ascending
        });

        Assert.That(
            page.Items.Select(r => r.Subject),
            Is.EqualTo(new[] { "Alpha", "Bravo", "Charlie" }).AsCollection);
    }

    [Test]
    public async Task GetPagedAsync_GivenAStatusFilter_ShouldReturnOnlyMatchingReferrals()
    {
        await GivenAReferral(reference: "REF-0001", status: ReferralStatus.New);
        await GivenAReferral(reference: "REF-0002", status: ReferralStatus.Closed);
        await GivenAReferral(reference: "REF-0003", status: ReferralStatus.Closed);

        PagedResult<Referral> page = await _sut.GetPagedAsync(
            new ReferralListQuery { Status = ReferralStatus.Closed });

        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(2));
            Assert.That(page.Items.Select(r => r.Status), Is.All.EqualTo(ReferralStatus.Closed));
        });
    }

    [Test]
    public async Task GetPagedAsync_GivenASearchTerm_ShouldMatchReferenceOrSubjectCaseInsensitively()
    {
        await GivenAReferral(reference: "REF-0001", subject: "Safeguarding concern");
        await GivenAReferral(reference: "REF-0002", subject: "Fraud investigation");
        await GivenAReferral(reference: "REF-0003", subject: "Unrelated matter");

        PagedResult<Referral> bySubject = await _sut.GetPagedAsync(new ReferralListQuery { Search = "SAFEGUARDING" });
        PagedResult<Referral> byReference = await _sut.GetPagedAsync(new ReferralListQuery { Search = "ref-0002" });

        Assert.Multiple(() =>
        {
            Assert.That(bySubject.TotalCount, Is.EqualTo(1));
            Assert.That(bySubject.Items[0].ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(byReference.TotalCount, Is.EqualTo(1));
            Assert.That(byReference.Items[0].ReferralReference, Is.EqualTo("REF-0002"));
        });
    }

    [Test]
    public async Task GetPagedAsync_GivenBothSearchAndStatus_ShouldApplyBothFilters()
    {
        await GivenAReferral(reference: "REF-0001", subject: "Safeguarding concern", status: ReferralStatus.New);
        await GivenAReferral(reference: "REF-0002", subject: "Safeguarding review", status: ReferralStatus.Closed);

        PagedResult<Referral> page = await _sut.GetPagedAsync(new ReferralListQuery
        {
            Search = "safeguarding",
            Status = ReferralStatus.Closed
        });

        Assert.Multiple(() =>
        {
            Assert.That(page.TotalCount, Is.EqualTo(1));
            Assert.That(page.Items[0].ReferralReference, Is.EqualTo("REF-0002"));
        });
    }

    // ------------------------------------------------------ Add / Remove

    [Test]
    public async Task Add_ShouldNotPersistUntilTheUnitOfWorkCommits()
    {
        Referral referral = Referral.Create(
            "REF-0001", "A subject", null, ReferralStatus.New, CreatedUtc.AddDays(-1), CreatedUtc);

        _sut.Add(referral);

        // Staged on the change tracker, not yet written.
        Assert.That(await _context.Referrals.CountAsync(), Is.Zero);

        await _context.SaveChangesAsync();

        Assert.That(await _context.Referrals.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Remove_ShouldDeleteTheReferralOnCommit()
    {
        Referral referral = await GivenAReferral();
        Referral tracked = (await _sut.GetByIdAsync(referral.Id))!;

        _sut.Remove(tracked);
        await _context.SaveChangesAsync();

        Assert.That(await _sut.GetByIdAsync(referral.Id), Is.Null);
    }
}

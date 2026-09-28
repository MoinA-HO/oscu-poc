using Microsoft.Extensions.Logging.Abstractions;
using OSCU.Application.Common.Core;
using OSCU.Application.Common.Interfaces;
using OSCU.Application.Common.Models;
using OSCU.Application.Features.Referrals;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Application.UnitTests.Features.Referrals;

[TestFixture]
public class ReferralServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReceivedUtc = new(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc);

    private Mock<IReferralRepository> _repository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IDateTimeProvider> _clock = null!;
    private ReferralService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IReferralRepository>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _clock = new Mock<IDateTimeProvider>();
        _clock.SetupGet(c => c.UtcNow).Returns(FixedNow);

        _sut = new ReferralService(
            _repository.Object,
            _unitOfWork.Object,
            _clock.Object,
            NullLogger<ReferralService>.Instance);
    }

    private static Referral AReferral(string reference = "REF-0001", string status = ReferralStatus.New) =>
        Referral.Create(reference, "A subject", "A description", status, ReceivedUtc, FixedNow);

    private static CreateReferralRequest ACreateRequest(
        string reference = "REF-0001",
        string status = ReferralStatus.New,
        DateTime? receivedDate = null) =>
        new(reference, "A subject", "A description", status, receivedDate ?? ReceivedUtc);

    // ---------------------------------------------------------------- Create

    [Test]
    public async Task CreateAsync_GivenUniqueReference_ShouldPersistAndReturnTheCreatedReferral()
    {
        Referral? persisted = null;
        _repository.Setup(r => r.ReferenceExistsAsync("REF-0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.Add(It.IsAny<Referral>()))
            .Callback<Referral>(r => persisted = r);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Result<ReferralResponse> result = await _sut.CreateAsync(ACreateRequest(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(result.Data.Subject, Is.EqualTo("A subject"));
            Assert.That(persisted, Is.Not.Null);
        });

        _repository.Verify(r => r.Add(It.IsAny<Referral>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ShouldStampCreatedDateFromTheClockNotTheRequest()
    {
        _repository.Setup(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.Add(It.IsAny<Referral>()));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Result<ReferralResponse> result = await _sut.CreateAsync(ACreateRequest(), CancellationToken.None);

        // CreatedDate is server-controlled: a client cannot backdate a referral.
        // Pinning the injected clock is what lets this assert an exact instant
        // rather than a tolerance window that would flake on a slow agent.
        Assert.That(result.Data!.CreatedDate, Is.EqualTo(FixedNow));
    }

    [Test]
    public async Task CreateAsync_GivenDuplicateReference_ShouldReturnConflictAndNotPersist()
    {
        _repository.Setup(r => r.ReferenceExistsAsync("REF-0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<ReferralResponse> result = await _sut.CreateAsync(ACreateRequest(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error!.Code, Is.EqualTo("Referral.DuplicateReference"));
            Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Conflict));
        });

        _repository.Verify(r => r.Add(It.IsAny<Referral>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_GivenNonUtcReceivedDate_ShouldNormaliseToUtcBeforePersisting()
    {
        // A client posting "2026-08-01T09:30:00" with no offset deserialises as
        // DateTimeKind.Unspecified, which Npgsql rejects for timestamptz.
        DateTime unspecified = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 9, 30, 0), DateTimeKind.Unspecified);
        _repository.Setup(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(r => r.Add(It.IsAny<Referral>()));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Result<ReferralResponse> result = await _sut.CreateAsync(
            ACreateRequest(receivedDate: unspecified), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.ReceivedDate.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(result.Data.ReceivedDate, Is.EqualTo(ReceivedUtc));
        });
    }

    // --------------------------------------------------------------- GetById

    [Test]
    public async Task GetByIdAsync_GivenExistingReferral_ShouldReturnIt()
    {
        Referral referral = AReferral();
        _repository.Setup(r => r.GetByIdAsync(referral.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referral);

        Result<ReferralResponse> result = await _sut.GetByIdAsync(referral.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Id, Is.EqualTo(referral.Id));
            Assert.That(result.Data.ReferralReference, Is.EqualTo("REF-0001"));
        });
    }

    [Test]
    public async Task GetByIdAsync_GivenUnknownId_ShouldReturnNotFound()
    {
        Guid id = Guid.CreateVersion7();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Referral?)null);

        Result<ReferralResponse> result = await _sut.GetByIdAsync(id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error!.Code, Is.EqualTo("Referral.NotFound"));
            Assert.That(result.Error.Type, Is.EqualTo(ErrorType.NotFound));
        });
    }

    // ------------------------------------------------------------------ List

    [Test]
    public async Task GetAsync_ShouldProjectThePagedEntitiesOntoResponses()
    {
        ReferralListQuery query = new() { Page = 2, PageSize = 10 };
        PagedResult<Referral> page = new([AReferral("REF-0001"), AReferral("REF-0002")], 2, 10, 42);
        _repository.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(page);

        Result<PagedResult<ReferralResponse>> result = await _sut.GetAsync(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Items, Has.Count.EqualTo(2));
            Assert.That(result.Data.Items[0].ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(result.Data.TotalCount, Is.EqualTo(42));
            Assert.That(result.Data.Page, Is.EqualTo(2));
            Assert.That(result.Data.PageSize, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task GetAsync_GivenNoMatches_ShouldReturnSuccessWithAnEmptyPage()
    {
        // An empty result set is not an error. Returning a failure here would
        // push every caller into treating "no referrals yet" as a fault.
        ReferralListQuery query = new();
        _repository.Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Referral>.Empty(query.Page, query.PageSize));

        Result<PagedResult<ReferralResponse>> result = await _sut.GetAsync(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Items, Is.Empty);
            Assert.That(result.Data.TotalCount, Is.Zero);
        });
    }

    // ---------------------------------------------------------------- Update

    [Test]
    public async Task UpdateAsync_GivenExistingReferral_ShouldApplyChangesAndSave()
    {
        Referral referral = AReferral();
        DateTime newReceived = new(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc);
        _repository.Setup(r => r.GetByIdAsync(referral.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referral);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        UpdateReferralRequest request = new("Revised subject", "Revised description", ReferralStatus.InProgress, newReceived);
        Result<ReferralResponse> result = await _sut.UpdateAsync(referral.Id, request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Subject, Is.EqualTo("Revised subject"));
            Assert.That(result.Data.Status, Is.EqualTo(ReferralStatus.InProgress));
            Assert.That(result.Data.ReceivedDate, Is.EqualTo(newReceived));
            // The reference is not amendable, so it survives the update.
            Assert.That(result.Data.ReferralReference, Is.EqualTo("REF-0001"));
        });

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_GivenUnknownId_ShouldReturnNotFoundAndNotSave()
    {
        Guid id = Guid.CreateVersion7();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Referral?)null);

        UpdateReferralRequest request = new("Revised subject", null, ReferralStatus.Closed, ReceivedUtc);
        Result<ReferralResponse> result = await _sut.UpdateAsync(id, request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.NotFound));
        });

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- Delete

    [Test]
    public async Task DeleteAsync_GivenExistingReferral_ShouldRemoveAndSave()
    {
        Referral referral = AReferral();
        _repository.Setup(r => r.GetByIdAsync(referral.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referral);
        _repository.Setup(r => r.Remove(referral));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Result result = await _sut.DeleteAsync(referral.Id, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _repository.Verify(r => r.Remove(referral), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_GivenUnknownId_ShouldReturnNotFoundAndNotSave()
    {
        Guid id = Guid.CreateVersion7();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Referral?)null);

        Result result = await _sut.DeleteAsync(id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error!.Type, Is.EqualTo(ErrorType.NotFound));
        });

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

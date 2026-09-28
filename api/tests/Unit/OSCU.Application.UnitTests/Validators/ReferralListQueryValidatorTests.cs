using FluentValidation.Results;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Application.Features.Referrals.Validators;

namespace OSCU.Application.UnitTests.Validators;

[TestFixture]
public class ReferralListQueryValidatorTests
{
    private ReferralListQueryValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new ReferralListQueryValidator();

    [Test]
    public void Validate_GivenDefaults_ShouldPass()
    {
        ValidationResult result = _sut.Validate(new ReferralListQuery());

        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validate_GivenNonPositivePage_ShouldFail(int page)
    {
        ValidationResult result = _sut.Validate(new ReferralListQuery { Page = page });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ReferralListQuery.Page)), Is.True);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Validate_GivenNonPositivePageSize_ShouldFail(int pageSize)
    {
        ValidationResult result = _sut.Validate(new ReferralListQuery { PageSize = pageSize });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ReferralListQuery.PageSize)), Is.True);
    }

    [Test]
    public void Validate_GivenPageSizeAboveTheCap_ShouldFail()
    {
        // An uncapped page size is a denial-of-service vector: one request for
        // pageSize=10000000 will happily try to materialise the whole table.
        ValidationResult result = _sut.Validate(
            new ReferralListQuery { PageSize = ReferralListQuery.MaxPageSize + 1 });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ReferralListQuery.PageSize)), Is.True);
    }

    [Test]
    public void Validate_GivenPageSizeAtTheCap_ShouldPass()
    {
        ValidationResult result = _sut.Validate(
            new ReferralListQuery { PageSize = ReferralListQuery.MaxPageSize });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_GivenUnrecognisedStatusFilter_ShouldFail()
    {
        ValidationResult result = _sut.Validate(new ReferralListQuery { Status = "Marinated" });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ReferralListQuery.Status)), Is.True);
    }

    [Test]
    public void Validate_GivenNoStatusFilter_ShouldPass()
    {
        ValidationResult result = _sut.Validate(new ReferralListQuery { Status = null });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_GivenAnOverlongSearchTerm_ShouldFail()
    {
        ValidationResult result = _sut.Validate(new ReferralListQuery { Search = new string('x', 201) });

        Assert.That(result.Errors.Any(e => e.PropertyName == nameof(ReferralListQuery.Search)), Is.True);
    }
}

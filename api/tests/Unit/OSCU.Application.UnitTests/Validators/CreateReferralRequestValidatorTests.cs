using FluentValidation.Results;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Application.Features.Referrals.Validators;
using OSCU.Domain.Entities;

namespace OSCU.Application.UnitTests.Validators;

[TestFixture]
public class CreateReferralRequestValidatorTests
{
    private CreateReferralRequestValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new CreateReferralRequestValidator();

    private static CreateReferralRequest ARequest(
        string reference = "REF-0001",
        string subject = "A subject",
        string? description = "A description",
        string status = ReferralStatus.New,
        DateTime? receivedDate = null) =>
        new(reference, subject, description, status,
            receivedDate ?? new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc));

    private static bool HasErrorFor(ValidationResult result, string propertyName) =>
        result.Errors.Any(e => e.PropertyName == propertyName);

    [Test]
    public void Validate_GivenAValidRequest_ShouldPass()
    {
        ValidationResult result = _sut.Validate(ARequest());

        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_GivenMissingReference_ShouldFail(string reference)
    {
        ValidationResult result = _sut.Validate(ARequest(reference: reference));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReferralReference)), Is.True);
    }

    [Test]
    public void Validate_GivenReferenceExceedingMaxLength_ShouldFail()
    {
        ValidationResult result = _sut.Validate(
            ARequest(reference: new string('R', Referral.ReferralReferenceMaxLength + 1)));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReferralReference)), Is.True);
    }

    [TestCase("REF-0001")]
    [TestCase("REF-99999999")]
    public void Validate_GivenAWellFormedReference_ShouldPass(string reference)
    {
        ValidationResult result = _sut.Validate(ARequest(reference: reference));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReferralReference)), Is.False);
    }

    [TestCase("ref-0001")]
    [TestCase("REF0001")]
    [TestCase("REF-")]
    [TestCase("XYZ-0001")]
    [TestCase("REF-0001; DROP TABLE referrals")]
    public void Validate_GivenAMalformedReference_ShouldFail(string reference)
    {
        ValidationResult result = _sut.Validate(ARequest(reference: reference));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReferralReference)), Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_GivenMissingSubject_ShouldFail(string subject)
    {
        ValidationResult result = _sut.Validate(ARequest(subject: subject));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.Subject)), Is.True);
    }

    [Test]
    public void Validate_GivenSubjectExceedingMaxLength_ShouldFail()
    {
        ValidationResult result = _sut.Validate(
            ARequest(subject: new string('S', Referral.SubjectMaxLength + 1)));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.Subject)), Is.True);
    }

    [Test]
    public void Validate_GivenNullDescription_ShouldPass()
    {
        ValidationResult result = _sut.Validate(ARequest(description: null));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.Description)), Is.False);
    }

    [Test]
    public void Validate_GivenDescriptionExceedingMaxLength_ShouldFail()
    {
        ValidationResult result = _sut.Validate(
            ARequest(description: new string('D', Referral.DescriptionMaxLength + 1)));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.Description)), Is.True);
    }

    [Test]
    public void Validate_GivenUnrecognisedStatus_ShouldFailWithTheAllowedValuesListed()
    {
        ValidationResult result = _sut.Validate(ARequest(status: "Marinated"));

        Assert.Multiple(() =>
        {
            Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.Status)), Is.True);
            // The message names the permitted values so the caller can fix it
            // without reading the source.
            Assert.That(
                result.Errors.Single(e => e.PropertyName == nameof(CreateReferralRequest.Status)).ErrorMessage,
                Does.Contain(ReferralStatus.InProgress));
        });
    }

    [TestCase("new")]
    [TestCase("IN PROGRESS")]
    public void Validate_GivenAKnownStatusInAnyCase_ShouldPass(string status)
    {
        ValidationResult result = _sut.Validate(ARequest(status: status));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.Status)), Is.False);
    }

    [Test]
    public void Validate_GivenDefaultReceivedDate_ShouldFail()
    {
        // default(DateTime), written explicitly. The helper parameter is
        // DateTime?, so a bare `default` is null and quietly falls through to
        // the helper's valid fallback date — the test then asserted nothing.
        ValidationResult result = _sut.Validate(ARequest(receivedDate: default(DateTime)));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReceivedDate)), Is.True);
    }

    [Test]
    public void Validate_GivenReceivedDateInTheFuture_ShouldFail()
    {
        // A referral cannot be received tomorrow.
        ValidationResult result = _sut.Validate(
            ARequest(receivedDate: DateTime.UtcNow.AddDays(2)));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReceivedDate)), Is.True);
    }

    [Test]
    public void Validate_GivenReceivedDateSlightlyAheadOfNow_ShouldPass()
    {
        // Tolerates modest clock skew between the caller's host and ours.
        ValidationResult result = _sut.Validate(
            ARequest(receivedDate: DateTime.UtcNow.AddMinutes(2)));

        Assert.That(HasErrorFor(result, nameof(CreateReferralRequest.ReceivedDate)), Is.False);
    }
}

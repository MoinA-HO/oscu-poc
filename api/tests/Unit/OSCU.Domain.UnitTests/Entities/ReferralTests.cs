using OSCU.Domain.Entities;

namespace OSCU.Domain.UnitTests.Entities;

/// <summary>
/// The domain guards are a safety net, not the user-facing validation layer.
/// FluentValidation rejects bad input at the API boundary with a 400; these
/// guards exist so that a bug elsewhere in the stack can never persist a
/// Referral that violates its own invariants.
/// </summary>
[TestFixture]
public class ReferralTests
{
    private static readonly DateTime ReceivedUtc = new(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime CreatedUtc = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    private static Referral CreateSut(
        string reference = "REF-0001",
        string subject = "Safeguarding concern raised by partner agency",
        string? description = "Initial details supplied by the referring officer.",
        string status = ReferralStatus.New,
        DateTime? receivedDate = null,
        DateTime? createdDate = null) =>
        Referral.Create(
            reference,
            subject,
            description,
            status,
            receivedDate ?? ReceivedUtc,
            createdDate ?? CreatedUtc);

    [Test]
    public void Create_GivenValidValues_ShouldPopulateEveryField()
    {
        Referral sut = CreateSut();

        Assert.Multiple(() =>
        {
            Assert.That(sut.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(sut.ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(sut.Subject, Is.EqualTo("Safeguarding concern raised by partner agency"));
            Assert.That(sut.Description, Is.EqualTo("Initial details supplied by the referring officer."));
            Assert.That(sut.Status, Is.EqualTo(ReferralStatus.New));
            Assert.That(sut.ReceivedDate, Is.EqualTo(ReceivedUtc));
            Assert.That(sut.CreatedDate, Is.EqualTo(CreatedUtc));
        });
    }

    [Test]
    public void Create_ShouldGenerateDistinctVersion7Identifiers()
    {
        // Version 7 GUIDs are time-ordered. Random v4 keys fragment the
        // B-tree index in Postgres as the table grows, which matters because
        // this schema is heading for 20+ tables of real caseload.
        Referral first = CreateSut(reference: "REF-0001");
        Referral second = CreateSut(reference: "REF-0002");

        Assert.Multiple(() =>
        {
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
            Assert.That(first.Id.Version, Is.EqualTo(7));
            Assert.That(second.Id.Version, Is.EqualTo(7));
        });
    }

    [Test]
    public void Create_GivenSurroundingWhitespace_ShouldTrimTextFields()
    {
        Referral sut = CreateSut(reference: "  REF-0001  ", subject: "  A subject  ");

        Assert.Multiple(() =>
        {
            Assert.That(sut.ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(sut.Subject, Is.EqualTo("A subject"));
        });
    }

    [Test]
    public void Create_GivenNullDescription_ShouldBeAccepted()
    {
        Referral sut = CreateSut(description: null);

        Assert.That(sut.Description, Is.Null);
    }

    [Test]
    public void Create_GivenWhitespaceOnlyDescription_ShouldNormaliseToNull()
    {
        // Keeps "absent" as a single representation in the database rather than
        // an open choice between NULL and ''.
        Referral sut = CreateSut(description: "   ");

        Assert.That(sut.Description, Is.Null);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_GivenMissingReference_ShouldThrow(string? reference)
    {
        Assert.That(() => CreateSut(reference: reference!), Throws.InstanceOf<ArgumentException>());
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_GivenMissingSubject_ShouldThrow(string? subject)
    {
        Assert.That(() => CreateSut(subject: subject!), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Create_GivenReferenceExceedingMaxLength_ShouldThrow()
    {
        string tooLong = new('R', Referral.ReferralReferenceMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSut(reference: tooLong));
    }

    [Test]
    public void Create_GivenSubjectExceedingMaxLength_ShouldThrow()
    {
        string tooLong = new('S', Referral.SubjectMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSut(subject: tooLong));
    }

    [Test]
    public void Create_GivenDescriptionExceedingMaxLength_ShouldThrow()
    {
        string tooLong = new('D', Referral.DescriptionMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSut(description: tooLong));
    }

    [Test]
    public void Create_GivenUnrecognisedStatus_ShouldThrow()
    {
        Assert.That(() => CreateSut(status: "Marinated"), Throws.InstanceOf<ArgumentException>());
    }

    [TestCase(DateTimeKind.Local)]
    [TestCase(DateTimeKind.Unspecified)]
    public void Create_GivenNonUtcReceivedDate_ShouldThrow(DateTimeKind kind)
    {
        // Npgsql maps DateTime to `timestamp with time zone` and throws at
        // save time unless Kind is Utc. Failing here gives a clear message at
        // the boundary instead of an opaque provider exception on SaveChanges.
        DateTime received = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 9, 30, 0), kind);

        Assert.That(() => CreateSut(receivedDate: received), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Create_GivenNonUtcCreatedDate_ShouldThrow()
    {
        DateTime created = DateTime.SpecifyKind(new DateTime(2026, 8, 11, 12, 0, 0), DateTimeKind.Local);

        Assert.That(() => CreateSut(createdDate: created), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Update_GivenValidValues_ShouldChangeMutableFieldsOnly()
    {
        Referral sut = CreateSut();
        Guid originalId = sut.Id;
        DateTime originalCreated = sut.CreatedDate;
        DateTime newReceived = new(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc);

        sut.Update("Revised subject", "Revised description", ReferralStatus.InProgress, newReceived);

        Assert.Multiple(() =>
        {
            Assert.That(sut.Subject, Is.EqualTo("Revised subject"));
            Assert.That(sut.Description, Is.EqualTo("Revised description"));
            Assert.That(sut.Status, Is.EqualTo(ReferralStatus.InProgress));
            Assert.That(sut.ReceivedDate, Is.EqualTo(newReceived));

            // Identity, reference and creation stamp are immutable after Create.
            Assert.That(sut.Id, Is.EqualTo(originalId));
            Assert.That(sut.ReferralReference, Is.EqualTo("REF-0001"));
            Assert.That(sut.CreatedDate, Is.EqualTo(originalCreated));
        });
    }

    [Test]
    public void Update_GivenUnrecognisedStatus_ShouldThrowAndLeaveEntityUnchanged()
    {
        Referral sut = CreateSut();

        Assert.That(
            () => sut.Update("Revised subject", null, "Marinated", ReceivedUtc),
            Throws.InstanceOf<ArgumentException>());

        Assert.Multiple(() =>
        {
            Assert.That(sut.Subject, Is.EqualTo("Safeguarding concern raised by partner agency"));
            Assert.That(sut.Status, Is.EqualTo(ReferralStatus.New));
        });
    }

    [Test]
    public void Update_GivenMissingSubject_ShouldThrow()
    {
        Referral sut = CreateSut();

        Assert.That(
            () => sut.Update("  ", null, ReferralStatus.Closed, ReceivedUtc),
            Throws.InstanceOf<ArgumentException>());
    }
}

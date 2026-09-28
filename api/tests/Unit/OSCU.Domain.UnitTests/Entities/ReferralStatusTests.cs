using OSCU.Domain.Entities;

namespace OSCU.Domain.UnitTests.Entities;

[TestFixture]
public class ReferralStatusTests
{
    [Test]
    public void All_ShouldContainTheFullControlledVocabulary()
    {
        Assert.That(ReferralStatus.All, Is.EquivalentTo(new[]
        {
            ReferralStatus.New,
            ReferralStatus.InProgress,
            ReferralStatus.OnHold,
            ReferralStatus.Closed,
            ReferralStatus.Rejected
        }));
    }

    [TestCase("New")]
    [TestCase("In Progress")]
    [TestCase("Closed")]
    public void IsValid_GivenKnownStatus_ShouldReturnTrue(string status)
    {
        Assert.That(ReferralStatus.IsValid(status), Is.True);
    }

    [TestCase("new")]
    [TestCase("IN PROGRESS")]
    public void IsValid_GivenKnownStatusInDifferentCase_ShouldReturnTrue(string status)
    {
        // Case-insensitive so a client sending "new" is not rejected on a
        // technicality, while the entity still stores the canonical casing.
        Assert.That(ReferralStatus.IsValid(status), Is.True);
    }

    [TestCase("Marinated")]
    [TestCase("")]
    [TestCase(null)]
    public void IsValid_GivenUnknownStatus_ShouldReturnFalse(string? status)
    {
        Assert.That(ReferralStatus.IsValid(status), Is.False);
    }

    [TestCase("new", ExpectedResult = "New")]
    [TestCase("IN PROGRESS", ExpectedResult = "In Progress")]
    [TestCase("On Hold", ExpectedResult = "On Hold")]
    public string Canonicalise_GivenKnownStatus_ShouldReturnCanonicalCasing(string status)
    {
        return ReferralStatus.Canonicalise(status);
    }

    [Test]
    public void Canonicalise_GivenUnknownStatus_ShouldThrow()
    {
        Assert.That(() => ReferralStatus.Canonicalise("Marinated"), Throws.InstanceOf<ArgumentException>());
    }
}

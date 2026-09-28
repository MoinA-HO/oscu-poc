using System.Text.RegularExpressions;
using FluentValidation;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Application.Features.Referrals.Validators;

public partial class CreateReferralRequestValidator : AbstractValidator<CreateReferralRequest>
{
    public CreateReferralRequestValidator()
    {
        RuleFor(request => request.ReferralReference)
            .NotEmpty()
            .WithMessage("Enter a referral reference.")
            .MaximumLength(Referral.ReferralReferenceMaxLength)
            .Matches(ReferenceFormat())
            .WithMessage("Enter a referral reference in the format REF-0001.");

        RuleFor(request => request.Subject)
            .NotEmpty()
            .WithMessage("Enter a subject.")
            .MaximumLength(Referral.SubjectMaxLength);

        RuleFor(request => request.Description)
            .MaximumLength(Referral.DescriptionMaxLength);

        RuleFor(request => request.Status)
            .NotEmpty()
            .WithMessage("Select a status.")
            .Must(ReferralStatus.IsValid)
            .WithMessage($"Select a status from: {string.Join(", ", ReferralStatus.All)}.");

        RuleFor(request => request.ReceivedDate)
            .NotEmpty()
            .WithMessage("Enter the date the referral was received.")
            .Must(BeNoLaterThanNow)
            .WithMessage("The date received cannot be in the future.");
    }

    /// <summary>
    /// Allows a few minutes of tolerance so a caller whose clock runs slightly
    /// fast is not rejected for a skew they cannot see or control.
    /// </summary>
    private static bool BeNoLaterThanNow(DateTime receivedDate) =>
        receivedDate <= DateTime.UtcNow.AddMinutes(5);

    /// <summary>
    /// ASSUMPTION: references are <c>REF-</c> followed by at least four digits.
    /// The specification does not state a format. Confirm the real convention
    /// with the business and change this one constant — the tests in
    /// CreateReferralRequestValidatorTests document the current rule.
    /// </summary>
    [GeneratedRegex(@"^REF-\d{4,}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceFormat();
}

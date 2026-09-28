using FluentValidation;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Application.Features.Referrals.Validators;

public class UpdateReferralRequestValidator : AbstractValidator<UpdateReferralRequest>
{
    public UpdateReferralRequestValidator()
    {
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
            .Must(receivedDate => receivedDate <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("The date received cannot be in the future.");
    }
}

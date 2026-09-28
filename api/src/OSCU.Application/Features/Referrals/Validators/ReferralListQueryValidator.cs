using FluentValidation;
using OSCU.Application.Features.Referrals.Dtos;
using OSCU.Domain.Entities;

namespace OSCU.Application.Features.Referrals.Validators;

public class ReferralListQueryValidator : AbstractValidator<ReferralListQuery>
{
    public ReferralListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0)
            .WithMessage("Page must be 1 or greater.");

        RuleFor(query => query.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be 1 or greater.")
            // Without an upper bound, a single request for pageSize=10000000
            // will try to materialise the entire table.
            .LessThanOrEqualTo(ReferralListQuery.MaxPageSize)
            .WithMessage($"Page size cannot exceed {ReferralListQuery.MaxPageSize}.");

        RuleFor(query => query.Search)
            .MaximumLength(ReferralListQuery.MaxSearchLength);

        RuleFor(query => query.Status)
            .Must(ReferralStatus.IsValid)
            .When(query => !string.IsNullOrWhiteSpace(query.Status))
            .WithMessage($"Filter by a status from: {string.Join(", ", ReferralStatus.All)}.");
    }
}

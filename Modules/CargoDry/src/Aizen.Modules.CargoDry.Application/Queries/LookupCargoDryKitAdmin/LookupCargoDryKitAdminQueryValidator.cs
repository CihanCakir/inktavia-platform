using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Queries.LookupCargoDryKitAdmin;

public sealed class LookupCargoDryKitAdminQueryValidator : AbstractValidator<LookupCargoDryKitAdminQuery>
{
    public LookupCargoDryKitAdminQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Lookup query must not be empty.")
            .MaximumLength(200)
            .WithMessage("Lookup query must not exceed 200 characters.");
    }
}

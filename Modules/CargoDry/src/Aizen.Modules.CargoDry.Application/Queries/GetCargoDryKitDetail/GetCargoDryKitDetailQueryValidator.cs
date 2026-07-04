using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitDetail;

public sealed class GetCargoDryKitDetailQueryValidator : AbstractValidator<GetCargoDryKitDetailQuery>
{
    public GetCargoDryKitDetailQueryValidator()
    {
        RuleFor(x => x.KitId)
            .GreaterThan(0)
            .WithMessage("KitId must be a positive integer.");
    }
}

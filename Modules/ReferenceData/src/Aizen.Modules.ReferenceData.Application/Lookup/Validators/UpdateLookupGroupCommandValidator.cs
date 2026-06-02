using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Lookup.Commands;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Validators;

public sealed class UpdateLookupGroupCommandValidator : AizenValidator<UpdateLookupGroupCommand>
{
    public UpdateLookupGroupCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.SortOrder).GreaterThanOrEqualTo(0);
    }
}

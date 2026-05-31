using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Lookup.Commands;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Validators;

public sealed class CreateLookupGroupCommandValidator : AizenValidator<CreateLookupGroupCommand>
{
    public CreateLookupGroupCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.SortOrder).GreaterThanOrEqualTo(0);
    }
}

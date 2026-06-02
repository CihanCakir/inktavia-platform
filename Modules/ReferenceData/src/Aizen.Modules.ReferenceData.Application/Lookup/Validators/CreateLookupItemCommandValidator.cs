using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Lookup.Commands;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Validators;

public sealed class CreateLookupItemCommandValidator : AizenValidator<CreateLookupItemCommand>
{
    public CreateLookupItemCommandValidator()
    {
        RuleFor(x => x.Request.GroupCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.SortOrder).GreaterThanOrEqualTo(0);
    }
}

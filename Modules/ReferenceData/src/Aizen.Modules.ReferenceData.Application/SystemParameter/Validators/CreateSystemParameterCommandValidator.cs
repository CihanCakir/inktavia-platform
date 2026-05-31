using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Validators;

public sealed class CreateSystemParameterCommandValidator : AizenValidator<CreateSystemParameterCommand>
{
    public CreateSystemParameterCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotEmpty();
    }
}

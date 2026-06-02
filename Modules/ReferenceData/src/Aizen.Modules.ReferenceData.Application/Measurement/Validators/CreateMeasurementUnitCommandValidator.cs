using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Measurement.Commands;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Validators;

public sealed class CreateMeasurementUnitCommandValidator : AizenValidator<CreateMeasurementUnitCommand>
{
    public CreateMeasurementUnitCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Symbol).NotEmpty().MaximumLength(20);
    }
}

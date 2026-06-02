using FluentValidation;
using Aizen.Core.Validation;
using Aizen.Modules.ReferenceData.Application.Location.Commands;

namespace Aizen.Modules.ReferenceData.Application.Location.Validators;

public sealed class CreateCityCommandValidator : AizenValidator<CreateCityCommand>
{
    public CreateCityCommandValidator()
    {
        RuleFor(x => x.Request.CountryCode).NotEmpty().MaximumLength(3);
        RuleFor(x => x.Request.CityCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.Name).NotEmpty();
    }
}

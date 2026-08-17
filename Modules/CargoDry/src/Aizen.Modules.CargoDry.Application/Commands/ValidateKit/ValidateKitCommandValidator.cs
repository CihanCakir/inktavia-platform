using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.ValidateKit;

public sealed class ValidateKitCommandValidator : AbstractValidator<ValidateKitCommand>
{
    public ValidateKitCommandValidator()
    {
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BatchCode).NotEmpty().MaximumLength(30);
    }
}

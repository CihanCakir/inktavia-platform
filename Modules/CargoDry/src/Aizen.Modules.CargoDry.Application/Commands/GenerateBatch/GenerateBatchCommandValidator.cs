using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;

public sealed class GenerateBatchCommandValidator : AbstractValidator<GenerateBatchCommand>
{
    public GenerateBatchCommandValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Count).InclusiveBetween(1, 5000);
    }
}

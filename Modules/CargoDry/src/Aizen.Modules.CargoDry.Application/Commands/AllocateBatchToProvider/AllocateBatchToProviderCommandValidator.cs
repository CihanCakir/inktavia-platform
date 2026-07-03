using Aizen.Modules.CargoDry.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.AllocateBatchToProvider;

public sealed class AllocateBatchToProviderCommandValidator
    : AbstractValidator<AllocateBatchToProviderCommand>
{
    public AllocateBatchToProviderCommandValidator()
    {
        RuleFor(x => x.BatchCode)
            .NotEmpty().WithMessage("BatchCode is required.");

        RuleFor(x => x.ProviderProfileId)
            .GreaterThan(0).WithMessage("ProviderProfileId must be a positive value.");

        RuleFor(x => x.CommercialModel)
            .IsInEnum().WithMessage("CommercialModel must be a valid value.");

        RuleFor(x => x.SalesChannel)
            .IsInEnum().WithMessage("SalesChannel must be a valid value.");

        // Consignment agreement is required when channel is ConsignmentSellThrough
        RuleFor(x => x.ConsignmentAgreementId)
            .Must((cmd, id) => id.HasValue || cmd.SalesChannel != SalesChannel.ConsignmentSellThrough)
            .WithMessage(
                "ConsignmentAgreementId is required for ConsignmentSellThrough channel, " +
                "or an active agreement must exist for the provider+product pair.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.");
    }
}

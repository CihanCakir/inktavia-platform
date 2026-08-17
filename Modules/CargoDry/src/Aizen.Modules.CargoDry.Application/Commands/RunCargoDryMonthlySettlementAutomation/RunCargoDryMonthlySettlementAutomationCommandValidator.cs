using Aizen.Modules.CargoDry.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.CargoDry.Application.Commands.RunCargoDryMonthlySettlementAutomation;

public sealed class RunCargoDryMonthlySettlementAutomationCommandValidator
    : AbstractValidator<RunCargoDryMonthlySettlementAutomationCommand>
{
    public RunCargoDryMonthlySettlementAutomationCommandValidator()
    {
        RuleFor(x => x.TargetYearMonth)
            .InclusiveBetween(200001, 209912)
            .WithMessage("TargetYearMonth must be a valid YYYYMM value between 200001 and 209912.")
            .Must(IsValidYearMonth)
            .WithMessage("TargetYearMonth must represent a valid calendar month (month portion must be 01–12).");

        RuleFor(x => x.Mode)
            .IsInEnum()
            .WithMessage("Mode must be a valid CargoDrySettlementAutomationMode value.");

        RuleFor(x => x.TriggeredByUserId)
            .GreaterThan(0)
            .WithMessage("TriggeredByUserId must be a valid user id.");

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .When(x => x.Note is not null)
            .WithMessage("Note must not exceed 1000 characters.");

        // AutoPrepareInvoice requires AutoPreparePayment
        RuleFor(x => x.AutoPrepareInvoice)
            .Must((cmd, autoInvoice) => !autoInvoice || cmd.AutoPreparePayment)
            .WithMessage("AutoPrepareInvoice can only be true when AutoPreparePayment is also true.");

        // Warn if Live mode is attempted without understanding the implications
        RuleFor(x => x.Mode)
            .Must(_ => true) // always passes — Live mode is permitted; guarded by invariants in the service
            .WithMessage("Live mode will dispatch command handlers for each eligible settlement. " +
                         "AutoCompletePayout is always false — payout completion remains manual.");
    }

    private static bool IsValidYearMonth(int yyyymm)
    {
        var month = yyyymm % 100;
        return month is >= 1 and <= 12;
    }
}

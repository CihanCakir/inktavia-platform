using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// Thrown by <see cref="PaymentEconomicsSnapshotEntity.Create"/> when any of the §4 economic
/// invariants fails. Zero tolerance: comparisons are exact equality on rounded decimals — there
/// is no 0.01 epsilon. The message names the specific invariant that was violated so a buggy
/// caller (P8 economics computation) can be diagnosed immediately.
/// </summary>
public sealed class PaymentEconomicsInvariantException : AizenBusinessException
{
    public PaymentEconomicsInvariantException(string invariant)
        : base((int)PaymentErrorCode.PaymentEconomicsInvariantViolation,
               $"Payment economics invariant violated: {invariant}")
    {
    }
}

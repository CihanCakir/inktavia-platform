using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.RecordChargeback;

/// <summary>
/// BE-P10 §21.2 — records a gateway chargeback (up to 13 months post-transaction). Marks the transaction disputed, runs
/// the §7.3 release-after recovery against the provider (clawback → negative-balance ledger), and books a distinct
/// <see cref="RecordChargebackResult.ChargebackExpenseAmount"/>. Idempotent on <see cref="GatewayChargebackReference"/>.
/// </summary>
public sealed class RecordChargebackCommand : AizenCommand<RecordChargebackResult>
{
    public required long   TransactionId              { get; init; }
    public required string GatewayChargebackReference { get; init; }
    /// <summary>The disputed (charged-back) gateway amount. Defaults to the full transaction gross when omitted (≤ 0).</summary>
    public          decimal ChargebackAmount          { get; init; }
    /// <summary>The separate gateway chargeback fee/expense (never deducted from the customer). Reporting = P12.</summary>
    public          decimal ChargebackExpenseAmount   { get; init; }
    public          string? Notes                     { get; init; }
}

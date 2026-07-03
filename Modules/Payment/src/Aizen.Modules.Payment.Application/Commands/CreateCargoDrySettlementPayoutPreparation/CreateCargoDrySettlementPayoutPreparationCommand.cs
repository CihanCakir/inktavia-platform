using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementPayoutPreparation;

/// <summary>
/// Creates a PayoutRecord preparation entry for a CargoDry sell-through monthly settlement.
/// This command is dispatched in-process by the CargoDry module via ISender.
///
/// Idempotent: if a PayoutRecord already exists for the given SourceSettlementId,
/// returns the existing record without creating a duplicate.
///
/// Does NOT call Iyzico. Does NOT execute any payout transfer.
/// Phase 4B (July 2026).
/// </summary>
public sealed class CreateCargoDrySettlementPayoutPreparationCommand
    : AizenCommand<CreateCargoDrySettlementPayoutPreparationResult>
{
    public required long    SourceSettlementId  { get; init; }
    public required string  SettlementCode      { get; init; }
    public required long    ProviderProfileId   { get; init; }
    public required decimal Amount              { get; init; }
    public required string  CurrencyCode        { get; init; }
    public required string  Description         { get; init; }
    public required long    PreparedByUserId    { get; init; }
}

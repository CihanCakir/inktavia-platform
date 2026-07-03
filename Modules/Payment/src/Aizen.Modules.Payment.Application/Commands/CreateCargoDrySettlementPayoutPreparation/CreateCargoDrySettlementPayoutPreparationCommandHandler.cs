using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementPayoutPreparation;

[DocumentationInfo("CreateCargoDrySettlementPayoutPreparationCommandHandler",
    "Creates a PayoutRecord preparation entry for a CargoDry sell-through monthly settlement " +
    "dispatched in-process from the CargoDry module via ISender. " +
    "Idempotent by (SourceType='CargoDrySettlement', SourceId=settlementId). " +
    "Does NOT execute any Iyzico payout — this is preparation/bookkeeping only. " +
    "Phase 4B (July 2026).")]
public sealed class CreateCargoDrySettlementPayoutPreparationCommandHandler
    : AizenCommandHandler<CreateCargoDrySettlementPayoutPreparationCommand, CreateCargoDrySettlementPayoutPreparationResult>
{
    private const string SourceType = "CargoDrySettlement";

    private readonly IPayoutRecordRepository                                            _payouts;
    private readonly ILogger<CreateCargoDrySettlementPayoutPreparationCommandHandler>   _logger;

    public CreateCargoDrySettlementPayoutPreparationCommandHandler(
        IPayoutRecordRepository                                           payouts,
        ILogger<CreateCargoDrySettlementPayoutPreparationCommandHandler>  logger)
    {
        _payouts = payouts;
        _logger  = logger;
    }

    public override async Task<CreateCargoDrySettlementPayoutPreparationResult?> Handle(
        CreateCargoDrySettlementPayoutPreparationCommand request, CancellationToken ct)
    {
        // ── Idempotency guard ───────────────────────────────────────────────────
        var existing = await _payouts.GetBySourceAsync(SourceType, request.SourceSettlementId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "CargoDry settlement payout record already exists. " +
                "SettlementId={SettlementId} PayoutRecordId={PayoutId}",
                request.SourceSettlementId, existing.Id);

            return new CreateCargoDrySettlementPayoutPreparationResult(
                PayoutRecordId: existing.Id,
                Status:         existing.Status,
                AlreadyExisted: true);
        }

        // ── Create new payout preparation record ────────────────────────────────
        var payout = PayoutRecordEntity.CreateForCargoDrySettlement(
            providerProfileId: request.ProviderProfileId,
            settlementId:      request.SourceSettlementId,
            amount:            request.Amount,
            currencyCode:      request.CurrencyCode,
            description:       request.Description);

        await _payouts.AddAsync(payout, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "CargoDry settlement payout record created. " +
            "SettlementId={SettlementId} SettlementCode={Code} Amount={Amount} {Currency}",
            request.SourceSettlementId, request.SettlementCode,
            request.Amount, request.CurrencyCode);

        return new CreateCargoDrySettlementPayoutPreparationResult(
            PayoutRecordId: payout.Id,
            Status:         payout.Status,
            AlreadyExisted: false);
    }
}

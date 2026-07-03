using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementPayoutPreparation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Implements ICargoDrySettlementPayoutService by dispatching
/// CreateCargoDrySettlementPayoutPreparationCommand via ISender.
///
/// This service bridges the module boundary: CargoDry.Application injects
/// ICargoDrySettlementPayoutService (from Payment.Abstraction) without needing a
/// compile-time reference to Payment.Application.
///
/// Phase 4B (July 2026).
/// </summary>
[DocumentationInfo("CargoDrySettlementPayoutService",
    "Cross-module service that allows the CargoDry module to prepare payout records " +
    "for monthly sell-through settlements by dispatching an in-process MediatR command " +
    "to the Payment module handler. Does NOT execute any real payout transfer.")]
public sealed class CargoDrySettlementPayoutService : ICargoDrySettlementPayoutService
{
    private readonly ISender                                _sender;
    private readonly ILogger<CargoDrySettlementPayoutService> _logger;

    public CargoDrySettlementPayoutService(
        ISender                                   sender,
        ILogger<CargoDrySettlementPayoutService>  logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<CreateCargoDrySettlementPayoutPreparationResult> PrepareSettlementPayoutAsync(
        long              settlementId,
        string            settlementCode,
        long              providerProfileId,
        decimal           amount,
        string            currencyCode,
        string            description,
        long              preparedByUserId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Preparing payout for CargoDry settlement. SettlementId={Id} Code={Code} Amount={Amount} {Currency}",
            settlementId, settlementCode, amount, currencyCode);

        var command = new CreateCargoDrySettlementPayoutPreparationCommand
        {
            SourceSettlementId = settlementId,
            SettlementCode     = settlementCode,
            ProviderProfileId  = providerProfileId,
            Amount             = amount,
            CurrencyCode       = currencyCode,
            Description        = description,
            PreparedByUserId   = preparedByUserId,
        };

        var result = await _sender.Send(command, ct)
            ?? throw new InvalidOperationException(
                $"CreateCargoDrySettlementPayoutPreparationCommand returned null for settlement {settlementId}.");

        return result;
    }
}

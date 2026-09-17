using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Split-host implementation of <see cref="ICargoDrySettlementPayoutService"/>: reaches the Payment module over HTTP
/// (<see cref="ICargoDrySettlementPaymentRemoteCall"/>) instead of the in-process MediatR bridge. Registered with TryAdd
/// so a co-hosted host — where Payment.Application registers the in-process
/// <c>CargoDrySettlementPayoutService</c> — always wins (no self-HTTP hop); the standalone aizen-cargodry pod, which
/// lacks Payment.Application, resolves this one. Keeps the seam synchronous, so the calling handler still receives the
/// real PayoutRecordId in one call and can <c>MarkPaymentPrepared</c> immediately (no handler/BFF/FE rework).
/// </summary>
[DocumentationInfo("RemoteCargoDrySettlementPayoutService",
    "HTTP bridge from CargoDry to Payment for settlement payout preparation in the split-host topology.")]
public sealed class RemoteCargoDrySettlementPayoutService : ICargoDrySettlementPayoutService
{
    private readonly ICargoDrySettlementPaymentRemoteCall              _remote;
    private readonly ILogger<RemoteCargoDrySettlementPayoutService>    _logger;

    public RemoteCargoDrySettlementPayoutService(
        ICargoDrySettlementPaymentRemoteCall           remote,
        ILogger<RemoteCargoDrySettlementPayoutService> logger)
    {
        _remote = remote;
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
            "Preparing CargoDry settlement payout via Payment remote call. SettlementId={Id} Code={Code} Amount={Amount} {Currency}",
            settlementId, settlementCode, amount, currencyCode);

        var response = await _remote.PrepareSettlementPayoutAsync(new PrepareCargoDrySettlementPayoutRemoteCallRequest
        {
            SourceSettlementId = settlementId,
            SettlementCode     = settlementCode,
            ProviderProfileId  = providerProfileId,
            Amount             = amount,
            CurrencyCode       = currencyCode,
            Description        = description,
            PreparedByUserId   = preparedByUserId,
        }, ct) ?? throw new InvalidOperationException(
            $"Payment remote call returned no payout result for settlement {settlementId}.");

        return new CreateCargoDrySettlementPayoutPreparationResult(
            response.PayoutRecordId,
            response.Status,
            response.AlreadyExisted);
    }
}

using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Split-host implementation of <see cref="ICargoDrySettlementPayoutLifecycleService"/>: drives the payout-record
/// lifecycle over HTTP (<see cref="ICargoDrySettlementPaymentRemoteCall"/>) instead of the in-process Payment.Application
/// bridge. Registered with TryAdd so the co-hosted host's in-process service always wins; the standalone aizen-cargodry
/// pod resolves this one. These calls move the PayoutRecord only — the settlement's transition to Settled remains in the
/// CargoDry CompleteCargoDrySettlementPayout handler, which calls Complete here then MarkPayoutCompleted itself.
/// </summary>
[DocumentationInfo("RemoteCargoDrySettlementPayoutLifecycleService",
    "HTTP bridge from CargoDry to Payment for the settlement payout lifecycle in the split-host topology.")]
public sealed class RemoteCargoDrySettlementPayoutLifecycleService : ICargoDrySettlementPayoutLifecycleService
{
    private readonly ICargoDrySettlementPaymentRemoteCall                    _remote;
    private readonly ILogger<RemoteCargoDrySettlementPayoutLifecycleService> _logger;

    public RemoteCargoDrySettlementPayoutLifecycleService(
        ICargoDrySettlementPaymentRemoteCall                    remote,
        ILogger<RemoteCargoDrySettlementPayoutLifecycleService> logger)
    {
        _remote = remote;
        _logger = logger;
    }

    public Task<CargoDryPayoutLifecycleResultDto> GetPayoutStateAsync(
        long payoutRecordId, long sourceSettlementId, CancellationToken ct = default)
        => Send(_remote.GetPayoutStateAsync(payoutRecordId,
            new GetCargoDrySettlementPayoutStateRemoteCallRequest { SourceSettlementId = sourceSettlementId }, ct),
            payoutRecordId, "state");

    public Task<CargoDryPayoutLifecycleResultDto> ApproveAsync(
        long payoutRecordId, long approvedByUserId, string? note, CancellationToken ct = default)
        => Send(_remote.ApprovePayoutAsync(payoutRecordId,
            new ApproveCargoDrySettlementPayoutRemoteCallRequest { ApprovedByUserId = approvedByUserId, Note = note }, ct),
            payoutRecordId, "approve");

    public Task<CargoDryPayoutLifecycleResultDto> MarkProcessingAsync(
        long payoutRecordId, long processedByUserId, string? externalReference, string? note, CancellationToken ct = default)
        => Send(_remote.MarkPayoutProcessingAsync(payoutRecordId,
            new MarkProcessingCargoDrySettlementPayoutRemoteCallRequest
            {
                ProcessedByUserId = processedByUserId, ExternalReference = externalReference, Note = note,
            }, ct),
            payoutRecordId, "processing");

    public Task<CargoDryPayoutLifecycleResultDto> CompleteManualAsync(
        long payoutRecordId, long completedByUserId, string manualPaymentReference, string? note, CancellationToken ct = default)
        => Send(_remote.CompletePayoutManualAsync(payoutRecordId,
            new CompleteCargoDrySettlementPayoutRemoteCallRequest
            {
                CompletedByUserId = completedByUserId, ManualPaymentReference = manualPaymentReference, Note = note,
            }, ct),
            payoutRecordId, "complete");

    public Task<CargoDryPayoutLifecycleResultDto> FailAsync(
        long payoutRecordId, long failedByUserId, string failureReason, string? externalReference, string? note, CancellationToken ct = default)
        => Send(_remote.FailPayoutAsync(payoutRecordId,
            new FailCargoDrySettlementPayoutRemoteCallRequest
            {
                FailedByUserId = failedByUserId, FailureReason = failureReason, ExternalReference = externalReference, Note = note,
            }, ct),
            payoutRecordId, "fail");

    private async Task<CargoDryPayoutLifecycleResultDto> Send(
        Task<CargoDryPayoutLifecycleResultDto> call, long payoutRecordId, string op)
    {
        _logger.LogInformation("CargoDry payout lifecycle '{Op}' via Payment remote call. PayoutRecordId={Id}", op, payoutRecordId);
        return await call
            ?? throw new InvalidOperationException(
                $"Payment remote call returned no result for payout '{op}' (PayoutRecordId={payoutRecordId}).");
    }
}

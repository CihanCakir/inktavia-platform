using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Responses;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RecordCargoDrySupplySaleRetryBff;

/// <summary>
/// Dev/ops reconciliation — re-fires the CargoDry internal supply record-sale to repair an attribution whose
/// SalePrice/commission was never resolved (e.g. the resolver failed at completion). Idempotency is keyed on the SR
/// link, which never persisted for the broken row, so a re-fire proceeds. The AdminPanel BFF controller gates this to
/// non-production environments. OwnerUserId is deliberately 0 (the preferred-provider set-once is skipped for a repair).
/// </summary>
public sealed class RecordCargoDrySupplySaleRetryBffCommand : AizenCommand<RecordCargoDrySupplySaleRemoteResponse>
{
    public long    ServiceRequestId { get; init; }
    public long    KitId            { get; init; }
    public decimal SaleAmount       { get; init; }
    public string  CurrencyCode     { get; init; } = default!;
}

[DocumentationInfo("Retry CargoDry supply record-sale (BFF)",
    "Dev/ops passthrough to the CargoDry internal record-sale endpoint to repair attribution financials. Non-production only.")]
public sealed class RecordCargoDrySupplySaleRetryBffCommandHandler
    : AizenCommandHandler<RecordCargoDrySupplySaleRetryBffCommand, RecordCargoDrySupplySaleRemoteResponse>
{
    private readonly ICargoDryRemoteCall     _cargoDry;
    private readonly IAdminIdentityResolver  _resolver;
    private readonly IAdminIdentityHolder    _holder;

    public RecordCargoDrySupplySaleRetryBffCommandHandler(
        ICargoDryRemoteCall cargoDry, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _cargoDry = cargoDry;
        _resolver = resolver;
        _holder   = holder;
    }

    public override async Task<RecordCargoDrySupplySaleRemoteResponse?> Handle(
        RecordCargoDrySupplySaleRetryBffCommand request, CancellationToken ct)
    {
        // Resolve the acting admin's user id for the audit trail (FinancialResolvedByUserId). Best-effort → 0.
        await _resolver.ResolveAsync(ct);
        var resolvedByUserId = _holder.UserId ?? 0;

        return await _cargoDry.RecordSupplySaleInternalAsync(
            new RecordCargoDrySupplySaleRemoteRequest
            {
                KitId            = request.KitId,
                ServiceRequestId = request.ServiceRequestId,
                OwnerUserId      = 0, // repair path — preferred-provider set-once is skipped (guarded by OwnerUserId > 0)
                SaleAmount       = request.SaleAmount,
                CurrencyCode     = request.CurrencyCode,
                ResolvedByUserId = resolvedByUserId,
            }, ct);
    }
}

using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.StockRequestAdminBff;

// ── Approve ──────────────────────────────────────────────────────────────────
public sealed class ApproveCargoDryStockRequestBffCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long    RequestId              { get; init; }
    public string? DecisionNote           { get; init; }
    public string  BatchCode              { get; init; } = default!;
    public int     CommercialModel        { get; init; } = 2;
    public int     SalesChannel           { get; init; } = 3;
    public long?   ConsignmentAgreementId { get; init; }
    public long?   WarehouseId            { get; init; }
}

[DocumentationInfo("Approve CargoDry stock request (admin BFF)", "Passthrough to the module approve (allocate) endpoint; asserts the acting admin id.")]
public sealed class ApproveCargoDryStockRequestBffCommandHandler
    : AizenCommandHandler<ApproveCargoDryStockRequestBffCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly IAdminIdentityResolver _resolver;
    private readonly IAdminIdentityHolder _holder;

    public ApproveCargoDryStockRequestBffCommandHandler(
        ICargoDryRemoteCall cargoDry, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _cargoDry = cargoDry; _resolver = resolver; _holder = holder;
    }

    public override async Task<CargoDryStockRequestDto?> Handle(ApproveCargoDryStockRequestBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        return await _cargoDry.ApproveStockRequestAsync(request.RequestId, new ApproveStockRequestBffRequest
        {
            DecidedByUserId        = _holder.UserId ?? 0,
            DecisionNote           = request.DecisionNote,
            BatchCode              = request.BatchCode,
            CommercialModel        = request.CommercialModel,
            SalesChannel           = request.SalesChannel,
            ConsignmentAgreementId = request.ConsignmentAgreementId,
            WarehouseId            = request.WarehouseId,
        }, ct);
    }
}

// ── Ship ─────────────────────────────────────────────────────────────────────
public sealed class ShipCargoDryStockRequestBffCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long   RequestId    { get; init; }
    public string TrackingCode { get; init; } = default!;
}

[DocumentationInfo("Ship CargoDry stock request (admin BFF)", "Passthrough to the module ship endpoint; asserts the acting admin id.")]
public sealed class ShipCargoDryStockRequestBffCommandHandler
    : AizenCommandHandler<ShipCargoDryStockRequestBffCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly IAdminIdentityResolver _resolver;
    private readonly IAdminIdentityHolder _holder;

    public ShipCargoDryStockRequestBffCommandHandler(
        ICargoDryRemoteCall cargoDry, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _cargoDry = cargoDry; _resolver = resolver; _holder = holder;
    }

    public override async Task<CargoDryStockRequestDto?> Handle(ShipCargoDryStockRequestBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        return await _cargoDry.ShipStockRequestAsync(request.RequestId, new ShipStockRequestBffRequest
        {
            ShippedByUserId = _holder.UserId ?? 0,
            TrackingCode    = request.TrackingCode,
        }, ct);
    }
}

// ── Reject ───────────────────────────────────────────────────────────────────
public sealed class RejectCargoDryStockRequestBffCommand : AizenCommand<CargoDryStockRequestDto>
{
    public long   RequestId { get; init; }
    public string Reason    { get; init; } = default!;
}

[DocumentationInfo("Reject CargoDry stock request (admin BFF)", "Passthrough to the module reject endpoint; asserts the acting admin id.")]
public sealed class RejectCargoDryStockRequestBffCommandHandler
    : AizenCommandHandler<RejectCargoDryStockRequestBffCommand, CargoDryStockRequestDto>
{
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly IAdminIdentityResolver _resolver;
    private readonly IAdminIdentityHolder _holder;

    public RejectCargoDryStockRequestBffCommandHandler(
        ICargoDryRemoteCall cargoDry, IAdminIdentityResolver resolver, IAdminIdentityHolder holder)
    {
        _cargoDry = cargoDry; _resolver = resolver; _holder = holder;
    }

    public override async Task<CargoDryStockRequestDto?> Handle(RejectCargoDryStockRequestBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        return await _cargoDry.RejectStockRequestAsync(request.RequestId, new RejectStockRequestBffRequest
        {
            DecidedByUserId = _holder.UserId ?? 0,
            Reason          = request.Reason,
        }, ct);
    }
}

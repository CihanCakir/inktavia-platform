using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.PartCommercialTerm;

// ─── PartCommercialTerm: Create (= append a new version) ─────────────────────
public sealed class CreatePartCommercialTermBffCommand : AizenCommand<CreatePartCommercialTermBffResponse>
{
    public CreatePartCommercialTermBffRequest Body { get; init; } = default!;
}
public sealed class CreatePartCommercialTermBffResponse { public PartCommercialTermCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create part commercial term BFF command handler (BE-S5)",
    "Forwards a new part commercial term (POST /part-commercial-term/rules). Versioning is a side effect — a new version " +
    "row is appended for the scope. PartCommercialTermConflict/Invalid surfaces through the envelope. Admin-only.")]
public sealed class CreatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<CreatePartCommercialTermBffCommand, CreatePartCommercialTermBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public CreatePartCommercialTermBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<CreatePartCommercialTermBffResponse?> Handle(CreatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePartCommercialTermAsync(request.Body, ct) };
}

// ─── PartCommercialTerm: Update (in place; scope + version immutable) ─────────
public sealed class UpdatePartCommercialTermBffCommand : AizenCommand<UpdatePartCommercialTermBffResponse>
{
    public long                              Id   { get; init; }
    public UpdatePartCommercialTermBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePartCommercialTermBffResponse { public PartCommercialTermUpdateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Update part commercial term BFF command handler (BE-S5)",
    "Forwards a part commercial term update (PUT /part-commercial-term/rules/{id}). Conflict/Invalid surfaces through the envelope. Admin-only.")]
public sealed class UpdatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<UpdatePartCommercialTermBffCommand, UpdatePartCommercialTermBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public UpdatePartCommercialTermBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePartCommercialTermBffResponse?> Handle(UpdatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePartCommercialTermAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}

// ─── PartCommercialTerm: Deactivate ──────────────────────────────────────────
public sealed class DeactivatePartCommercialTermBffCommand : AizenCommand<DeactivatePartCommercialTermBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePartCommercialTermBffResponse { public bool Result { get; init; } }

[DocumentationInfo("Deactivate part commercial term BFF command handler (BE-S5)",
    "Forwards a deactivate (POST /part-commercial-term/rules/{id}/deactivate). Admin-only.")]
public sealed class DeactivatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<DeactivatePartCommercialTermBffCommand, DeactivatePartCommercialTermBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public DeactivatePartCommercialTermBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePartCommercialTermBffResponse?> Handle(DeactivatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePartCommercialTermAsync(request.Id, ct) };
}

// ─── PartCommercialTerm: Reactivate ──────────────────────────────────────────
public sealed class ReactivatePartCommercialTermBffCommand : AizenCommand<ReactivatePartCommercialTermBffResponse>
{
    public long Id { get; init; }
}
public sealed class ReactivatePartCommercialTermBffResponse { public bool Result { get; init; } }

[DocumentationInfo("Reactivate part commercial term BFF command handler (BE-S5)",
    "Forwards a reactivate (POST /part-commercial-term/rules/{id}/reactivate). The module re-runs the overlap guard, so " +
    "PartCommercialTermConflict / PartCommercialTermNotInactive may surface through the envelope. Admin-only.")]
public sealed class ReactivatePartCommercialTermBffCommandHandler
    : AizenCommandHandler<ReactivatePartCommercialTermBffCommand, ReactivatePartCommercialTermBffResponse>
{
    private readonly IAdminPaymentBffRemoteCall _payment;
    public ReactivatePartCommercialTermBffCommandHandler(IAdminPaymentBffRemoteCall payment) => _payment = payment;

    public override async Task<ReactivatePartCommercialTermBffResponse?> Handle(ReactivatePartCommercialTermBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ReactivatePartCommercialTermAsync(request.Id, ct) };
}

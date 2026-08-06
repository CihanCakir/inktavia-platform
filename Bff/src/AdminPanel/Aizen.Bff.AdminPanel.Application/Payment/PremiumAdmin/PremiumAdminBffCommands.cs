using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.PremiumAdmin;

// ─── Create product ──────────────────────────────────────────────────────────
public sealed class CreatePremiumProductBffCommand : AizenCommand<CreatePremiumProductBffResponse>
{
    public CreatePremiumProductBffRequest Body { get; init; } = default!;
}
public sealed class CreatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Create premium product BFF command handler (P11)",
    "Forwards a new premium product to the Payment module (POST /admin/premium/products). EntitlementType is a string enum name.")]
public sealed class CreatePremiumProductBffCommandHandler
    : AizenCommandHandler<CreatePremiumProductBffCommand, CreatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreatePremiumProductBffResponse?> Handle(CreatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePremiumProductAsync(request.Body, ct) };
}

// ─── Update product ──────────────────────────────────────────────────────────
public sealed class UpdatePremiumProductBffCommand : AizenCommand<UpdatePremiumProductBffResponse>
{
    public long                          Id   { get; init; }
    public UpdatePremiumProductBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Update premium product BFF command handler (P11)",
    "Forwards a premium product update (PUT /admin/premium/products/{id}). Id is applied from the route by the module.")]
public sealed class UpdatePremiumProductBffCommandHandler
    : AizenCommandHandler<UpdatePremiumProductBffCommand, UpdatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePremiumProductBffResponse?> Handle(UpdatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePremiumProductAsync(request.Id, request.Body, ct) };
}

// ─── Activate product ────────────────────────────────────────────────────────
public sealed class ActivatePremiumProductBffCommand : AizenCommand<ActivatePremiumProductBffResponse>
{
    public long Id { get; init; }
}
public sealed class ActivatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Activate premium product BFF command handler (P11)",
    "Forwards an activate (POST /admin/premium/products/{id}/activate).")]
public sealed class ActivatePremiumProductBffCommandHandler
    : AizenCommandHandler<ActivatePremiumProductBffCommand, ActivatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public ActivatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<ActivatePremiumProductBffResponse?> Handle(ActivatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.ActivatePremiumProductAsync(request.Id, ct) };
}

// ─── Deactivate product ──────────────────────────────────────────────────────
public sealed class DeactivatePremiumProductBffCommand : AizenCommand<DeactivatePremiumProductBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePremiumProductBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Deactivate premium product BFF command handler (P11)",
    "Forwards a deactivate (POST /admin/premium/products/{id}/deactivate).")]
public sealed class DeactivatePremiumProductBffCommandHandler
    : AizenCommandHandler<DeactivatePremiumProductBffCommand, DeactivatePremiumProductBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivatePremiumProductBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePremiumProductBffResponse?> Handle(DeactivatePremiumProductBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePremiumProductAsync(request.Id, ct) };
}

// ─── Create price ────────────────────────────────────────────────────────────
public sealed class CreatePremiumProductPriceBffCommand : AizenCommand<CreatePremiumProductPriceBffResponse>
{
    public CreatePremiumProductPriceBffRequest Body { get; init; } = default!;
}
public sealed class CreatePremiumProductPriceBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Create premium product price BFF command handler (P11)",
    "Forwards a new versioned premium price (POST /admin/premium/prices).")]
public sealed class CreatePremiumProductPriceBffCommandHandler
    : AizenCommandHandler<CreatePremiumProductPriceBffCommand, CreatePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreatePremiumProductPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreatePremiumProductPriceBffResponse?> Handle(CreatePremiumProductPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreatePremiumProductPriceAsync(request.Body, ct) };
}

// ─── Update price ────────────────────────────────────────────────────────────
public sealed class UpdatePremiumProductPriceBffCommand : AizenCommand<UpdatePremiumProductPriceBffResponse>
{
    public long                               Id   { get; init; }
    public UpdatePremiumProductPriceBffRequest Body { get; init; } = default!;
}
public sealed class UpdatePremiumProductPriceBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Update premium product price BFF command handler (P11)",
    "Forwards a premium price update (PUT /admin/premium/prices/{id}). Id is applied from the route by the module.")]
public sealed class UpdatePremiumProductPriceBffCommandHandler
    : AizenCommandHandler<UpdatePremiumProductPriceBffCommand, UpdatePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdatePremiumProductPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdatePremiumProductPriceBffResponse?> Handle(UpdatePremiumProductPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdatePremiumProductPriceAsync(request.Id, request.Body, ct) };
}

// ─── Deactivate price ────────────────────────────────────────────────────────
public sealed class DeactivatePremiumProductPriceBffCommand : AizenCommand<DeactivatePremiumProductPriceBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivatePremiumProductPriceBffResponse { public PremiumMutateResultDto Result { get; init; } = default!; }

[DocumentationInfo("Deactivate premium product price BFF command handler (P11)",
    "Forwards a deactivate (POST /admin/premium/prices/{id}/deactivate).")]
public sealed class DeactivatePremiumProductPriceBffCommandHandler
    : AizenCommandHandler<DeactivatePremiumProductPriceBffCommand, DeactivatePremiumProductPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivatePremiumProductPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivatePremiumProductPriceBffResponse?> Handle(DeactivatePremiumProductPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivatePremiumProductPriceAsync(request.Id, ct) };
}

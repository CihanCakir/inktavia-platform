using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.ProviderPlanPrice;

// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreateProviderPlanPriceBffCommand : AizenCommand<CreateProviderPlanPriceBffResponse>
{
    public CreateProviderPlanPriceBffRequest Body { get; init; } = default!;
}
public sealed class CreateProviderPlanPriceBffResponse { public ProviderPlanPriceCreateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Create provider-plan-price BFF command handler (BE-P4)",
    "Forwards a new versioned plan price (POST /plan-prices). ProviderPlanPriceConflict/Gap surfaces through the envelope.")]
public sealed class CreateProviderPlanPriceBffCommandHandler
    : AizenCommandHandler<CreateProviderPlanPriceBffCommand, CreateProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public CreateProviderPlanPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<CreateProviderPlanPriceBffResponse?> Handle(CreateProviderPlanPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.CreateProviderPlanPriceAsync(request.Body, ct) };
}

// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdateProviderPlanPriceBffCommand : AizenCommand<UpdateProviderPlanPriceBffResponse>
{
    public long                             Id   { get; init; }
    public UpdateProviderPlanPriceBffRequest Body { get; init; } = default!;
}
public sealed class UpdateProviderPlanPriceBffResponse { public ProviderPlanPriceMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Update provider-plan-price BFF command handler (BE-P4)",
    "Forwards a plan-price update (PUT /plan-prices/{id}). Conflict/Gap surfaces through the envelope.")]
public sealed class UpdateProviderPlanPriceBffCommandHandler
    : AizenCommandHandler<UpdateProviderPlanPriceBffCommand, UpdateProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public UpdateProviderPlanPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<UpdateProviderPlanPriceBffResponse?> Handle(UpdateProviderPlanPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.UpdateProviderPlanPriceAsync(request.Id, request.Body with { Id = request.Id }, ct) };
}

// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateProviderPlanPriceBffCommand : AizenCommand<DeactivateProviderPlanPriceBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateProviderPlanPriceBffResponse { public ProviderPlanPriceMutateBffResult Result { get; init; } = default!; }

[DocumentationInfo("Deactivate provider-plan-price BFF command handler (BE-P4)",
    "Forwards a deactivate (POST /plan-prices/{id}/deactivate).")]
public sealed class DeactivateProviderPlanPriceBffCommandHandler
    : AizenCommandHandler<DeactivateProviderPlanPriceBffCommand, DeactivateProviderPlanPriceBffResponse>
{
    private readonly IPaymentRemoteCall _payment;
    public DeactivateProviderPlanPriceBffCommandHandler(IPaymentRemoteCall payment) => _payment = payment;

    public override async Task<DeactivateProviderPlanPriceBffResponse?> Handle(DeactivateProviderPlanPriceBffCommand request, CancellationToken ct)
        => new() { Result = await _payment.DeactivateProviderPlanPriceAsync(request.Id, ct) };
}

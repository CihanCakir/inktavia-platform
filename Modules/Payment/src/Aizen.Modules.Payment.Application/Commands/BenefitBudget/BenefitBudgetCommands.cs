using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Services;

namespace Aizen.Modules.Payment.Application.Commands.BenefitBudget;

// ── Reserve ──────────────────────────────────────────────────────────────────

public sealed class ReserveBenefitCommand : AizenCommand<BenefitOpResult>
{
    public required long    BudgetId   { get; init; }
    public required decimal Amount     { get; init; }
    public required string  ContextRef { get; init; }
}

[DocumentationInfo("ReserveBenefitCommandHandler",
    "Holds a benefit amount against a budget before checkout (§19.7). Idempotent per (budget, contextRef); throws " +
    "CustomerBenefitInsufficientRemaining if it exceeds Remaining and CustomerBenefitConcurrencyConflict on a clash.")]
public sealed class ReserveBenefitCommandHandler : AizenCommandHandler<ReserveBenefitCommand, BenefitOpResult>
{
    private readonly CustomerBenefitBudgetService _service;
    public ReserveBenefitCommandHandler(CustomerBenefitBudgetService service) => _service = service;

    public override async Task<BenefitOpResult?> Handle(ReserveBenefitCommand request, CancellationToken ct)
    {
        var r = await _service.ReserveAsync(request.BudgetId, request.Amount, request.ContextRef, ct);
        return new BenefitOpResult(r.Id, r.BudgetId, r.Amount, r.Status.ToString());
    }
}

// ── Consume ──────────────────────────────────────────────────────────────────

public sealed class ConsumeBenefitCommand : AizenCommand<BenefitOpResult>
{
    public required long ReservationId { get; init; }
}

[DocumentationInfo("ConsumeBenefitCommandHandler",
    "Converts a reservation to consumed on successful payment (§19.7). Idempotent — a duplicate consume is a no-op.")]
public sealed class ConsumeBenefitCommandHandler : AizenCommandHandler<ConsumeBenefitCommand, BenefitOpResult>
{
    private readonly CustomerBenefitBudgetService _service;
    public ConsumeBenefitCommandHandler(CustomerBenefitBudgetService service) => _service = service;

    public override async Task<BenefitOpResult?> Handle(ConsumeBenefitCommand request, CancellationToken ct)
    {
        var r = await _service.ConsumeAsync(request.ReservationId, ct);
        return new BenefitOpResult(r.Id, r.BudgetId, r.Amount, r.Status.ToString());
    }
}

// ── Release ──────────────────────────────────────────────────────────────────

public sealed class ReleaseBenefitCommand : AizenCommand<BenefitOpResult>
{
    public required long ReservationId { get; init; }
}

[DocumentationInfo("ReleaseBenefitCommandHandler",
    "Releases a reservation back to Remaining on failure/timeout/cancel (§19.7). Idempotent.")]
public sealed class ReleaseBenefitCommandHandler : AizenCommandHandler<ReleaseBenefitCommand, BenefitOpResult>
{
    private readonly CustomerBenefitBudgetService _service;
    public ReleaseBenefitCommandHandler(CustomerBenefitBudgetService service) => _service = service;

    public override async Task<BenefitOpResult?> Handle(ReleaseBenefitCommand request, CancellationToken ct)
    {
        var r = await _service.ReleaseAsync(request.ReservationId, ct);
        return new BenefitOpResult(r.Id, r.BudgetId, r.Amount, r.Status.ToString());
    }
}

public sealed record BenefitOpResult(long ReservationId, long BudgetId, decimal Amount, string Status);

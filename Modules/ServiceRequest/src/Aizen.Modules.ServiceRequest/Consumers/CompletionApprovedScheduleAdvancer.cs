using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Application.Command.Maintenance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Consumers;

/// <summary>
/// S12 — recompute-on-completion. Consumes the SR module's own <see cref="ServiceRequestCompletionApprovedMessage"/>
/// and advances the active maintenance schedule matching the completed SR's (vessel, category, type) via the
/// <see cref="AdvanceMaintenanceScheduleCommand"/>. Decoupled from the approval transaction (a schedule miss never
/// blocks completion). Auto-registered by the messagebus assembly scan (no manual DI). Separate from CargoDry.
/// </summary>
public sealed class CompletionApprovedScheduleAdvancer
    : AizenBaseMessageConsumer<ServiceRequestCompletionApprovedMessage>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompletionApprovedScheduleAdvancer> _logger;

    public CompletionApprovedScheduleAdvancer(IServiceProvider sp) : base(sp)
    {
        _scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        _logger       = sp.GetRequiredService<ILogger<CompletionApprovedScheduleAdvancer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        ServiceRequestCompletionApprovedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        ServiceRequestCompletionApprovedMessage message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cqrs = scope.ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
        await cqrs.ProcessAsync<AdvanceMaintenanceScheduleResponse>(
            new AdvanceMaintenanceScheduleCommand(message.ServiceRequestId), cancellationToken);
    }

    public override Task ExecuteRollbackMessage(
        ServiceRequestCompletionApprovedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "CompletionApprovedScheduleAdvancer rollback for SR {SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}

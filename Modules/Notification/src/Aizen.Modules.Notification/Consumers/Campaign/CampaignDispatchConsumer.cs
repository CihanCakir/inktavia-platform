using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Message;
using Aizen.Modules.Notification.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Campaign;

/// <summary>
/// Faz 28.6 — admin kampanyasını (NotificationCampaignQueuedMessage) yükleyip mevcut kanal dispatcher'ları üzerinden
/// dağıtır. Asıl iş <see cref="ICampaignDispatchService"/>'te (test edilebilir); consumer ince bir kabuktur. Assembly
/// taramasıyla otomatik kaydolur (BuilderExtensions.AddAizenMessagebus).
/// </summary>
public sealed class CampaignDispatchConsumer : AizenBaseMessageConsumer<NotificationCampaignQueuedMessage>
{
    private readonly ICampaignDispatchService _service;
    private readonly ILogger<CampaignDispatchConsumer> _logger;

    public CampaignDispatchConsumer(IServiceProvider sp) : base(sp)
    {
        _service = sp.GetRequiredService<ICampaignDispatchService>();
        _logger  = sp.GetRequiredService<ILogger<CampaignDispatchConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(NotificationCampaignQueuedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(NotificationCampaignQueuedMessage message, CancellationToken ct)
    {
        await _service.DispatchAsync(message.CampaignId, ct);
    }

    public override Task ExecuteRollbackMessage(
        NotificationCampaignQueuedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: CampaignDispatchConsumer for CampaignId={Id}: {Error}", message.CampaignId, ex.Message);
        return Task.CompletedTask;
    }
}

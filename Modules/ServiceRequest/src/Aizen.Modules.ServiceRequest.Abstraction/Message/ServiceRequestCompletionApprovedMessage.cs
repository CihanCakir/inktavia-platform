using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

[DocumentationInfo("Service request completion approved message", "Published when the owner approves the completion.")]
public sealed class ServiceRequestCompletionApprovedMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long CompletionId { get; set; }
    public long ProviderUserId { get; set; }

    /// <summary>
    /// BE_NF1 (D5) — the provider's PROFILE id (from the SR's assignment). Provider notifications/tokens/preferences are
    /// keyed by ProviderProfileId (as OfferCreated/AssignmentCreated do); keying by the raw ProviderUserId mis-files the
    /// notification for any provider whose UserId != ProfileId. Additive; defaults to 0 (consumer falls back to UserId).
    /// </summary>
    public long ProviderProfileId { get; set; }

    public long OwnerUserId { get; set; }
}

using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CancelProviderSubscription;

/// <summary>
/// Cancels an active provider subscription. The subscription remains valid until PeriodEnd
/// (no pro-rata refund by default in MVP). Status is set to Cancelled immediately.
/// </summary>
public sealed class CancelProviderSubscriptionCommand : AizenCommand<CancelSubscriptionResult>
{
    public required long    ProviderProfileId    { get; init; }
    public          string? CancellationReason  { get; init; }
}

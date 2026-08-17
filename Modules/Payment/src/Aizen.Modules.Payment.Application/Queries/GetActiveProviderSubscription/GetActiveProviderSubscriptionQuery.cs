using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetActiveProviderSubscription;

/// <summary>Returns the currently active provider subscription, or null if none exists.</summary>
public sealed class GetActiveProviderSubscriptionQuery
    : AizenQuery<ActiveProviderSubscriptionResult?>
{
    public required long ProviderProfileId { get; init; }
}

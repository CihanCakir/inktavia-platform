using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderSubscription;

public sealed class GetProviderSubscriptionBffQuery : AizenQuery<GetProviderSubscriptionBffResponse>
{
    public long ProviderProfileId { get; init; }
}

public sealed class GetProviderSubscriptionBffResponse
{
    public ActiveProviderSubscriptionResult? Subscription        { get; init; }
    /// <summary>Provider full name — resolved from Identity module by BFF in parallel with subscription fetch.</summary>
    public string?                           ProviderDisplayName { get; init; }
    /// <summary>Provider avatar URL — resolved from Identity module by BFF.</summary>
    public string?                           ProviderAvatarUrl   { get; init; }
}

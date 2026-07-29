using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderSplitEligibility;

/// <summary>BE-I1 — pure read of a provider's split-eligibility (the P9 gate signal). No state written.</summary>
public sealed class GetProviderSplitEligibilityQuery : AizenQuery<GetProviderSplitEligibilityRemoteCallResponse>
{
    public required GetProviderSplitEligibilityRemoteCallRequest Request { get; init; }
}

public sealed class GetProviderSplitEligibilityQueryHandler
    : AizenQueryHandler<GetProviderSplitEligibilityQuery, GetProviderSplitEligibilityRemoteCallResponse>
{
    private readonly IProviderPaymentProfileRepository _profiles;
    public GetProviderSplitEligibilityQueryHandler(IProviderPaymentProfileRepository profiles) => _profiles = profiles;

    public override async Task<GetProviderSplitEligibilityRemoteCallResponse?> Handle(
        GetProviderSplitEligibilityQuery query, CancellationToken ct)
    {
        var e = await _profiles.GetSplitEligibilityAsync(query.Request.ProviderProfileId, ct);

        return new GetProviderSplitEligibilityRemoteCallResponse
        {
            IsSplitEligible  = e.IsSplitEligible,
            OnboardingStatus = e.OnboardingStatus,
            HasProfile       = e.HasProfile,
            Reason           = e.IsSplitEligible ? null : DescribeReason(e),
        };
    }

    private static string DescribeReason(ProviderSplitEligibility e) => !e.HasProfile
        ? "Provider has no payment profile — sub-merchant not onboarded."
        : $"Provider sub-merchant not split-eligible (onboarding status: {e.OnboardingStatus}).";
}

using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

namespace Aizen.Bff.MarineProvider.Application.PartTerms;

/// <summary>
/// S5 — cost-free part-line allowance preview for the offer builder (compute-on-demand, nothing persisted). Returns the
/// max customer discount + funded split + min-receivable per Product/Consumable line. The response is the ONLY projection
/// of a PartCommercialTerm that leaves Payment: it carries NO supplier cost / dealer margin (§20.9 confidentiality).
/// </summary>
public sealed class GetOfferPartTermsPreviewBffQuery : AizenQuery<ResolvePartLineAllowancesRemoteCallResponse>
{
    public long ServiceRequestId { get; init; }
    public long OfferId          { get; init; }
}

public sealed class GetOfferPartTermsPreviewBffQueryHandler
    : AizenQueryHandler<GetOfferPartTermsPreviewBffQuery, ResolvePartLineAllowancesRemoteCallResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetOfferPartTermsPreviewBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest)
    {
        _resolver = resolver; _identityHolder = identityHolder; _serviceRequest = serviceRequest;
    }

    public override async Task<ResolvePartLineAllowancesRemoteCallResponse?> Handle(
        GetOfferPartTermsPreviewBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var result = await _serviceRequest.GetOfferPartTermsPreview(request.ServiceRequestId, request.OfferId);
        return result.Body;
    }
}

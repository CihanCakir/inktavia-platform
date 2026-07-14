using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class WithdrawOfferBffCommandHandler
    : AizenCommandHandler<WithdrawOfferBffCommand, WithdrawOfferBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IProviderServiceRequestRemoteCall _serviceRequest;

    public WithdrawOfferBffCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IProviderServiceRequestRemoteCall serviceRequest)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
    }

    public override async Task<WithdrawOfferBffResponse?> Handle(WithdrawOfferBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var result = await _serviceRequest.WithdrawOffer(
            request.ServiceRequestId, request.OfferId,
            new WithdrawServiceRequestOfferRequest { Reason = request.Reason });

        return new WithdrawOfferBffResponse
        {
            Success = result.Header?.IsSuccess == true,
            Message = result.Header?.IsSuccess != true ? result.Header?.ErrorMessage : null,
        };
    }
}

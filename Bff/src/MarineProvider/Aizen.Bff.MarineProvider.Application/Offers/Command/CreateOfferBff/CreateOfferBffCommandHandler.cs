using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class CreateOfferBffCommandHandler
    : AizenCommandHandler<CreateOfferBffCommand, CreateOfferBffResponse>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _identityHolder;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public CreateOfferBffCommandHandler(
        IProviderProfileResolver resolver,
        IProviderIdentityHolder identityHolder,
        IServiceRequestRemoteCall serviceRequest)
    {
        _resolver = resolver;
        _identityHolder = identityHolder;
        _serviceRequest = serviceRequest;
    }

    public override async Task<CreateOfferBffResponse?> Handle(CreateOfferBffCommand request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_identityHolder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        request.Body.ProviderProfileId = _identityHolder.ProfileId.Value;

        var result = await _serviceRequest.CreateOffer(request.ServiceRequestId, request.Body);

        if (result.Header?.IsSuccess != true)
            return new CreateOfferBffResponse
            {
                Success = false,
                Message = result.Header?.ErrorMessage ?? "Failed to create offer.",
            };

        return new CreateOfferBffResponse
        {
            Success = true,
            OfferId = result.Body?.Offer?.Id,
        };
    }
}

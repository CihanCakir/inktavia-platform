using Aizen.Bff.MarineProvider.Application.Common;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.MarineProvider.Application.Catalog;

public sealed class DeleteOfferCatalogItemBffCommandHandler
    : AizenCommandHandler<DeleteOfferCatalogItemBffCommand, BffSuccessResult>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IServiceRequestRemoteCall _sr;

    public DeleteOfferCatalogItemBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IServiceRequestRemoteCall sr)
    { _resolver = r; _h = h; _sr = sr; }

    public override async Task<BffSuccessResult?> Handle(DeleteOfferCatalogItemBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        await _sr.DeleteCatalogItem(cmd.Id);
        return new BffSuccessResult();
    }
}

using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Template;

public sealed class UpdateOfferTemplateBffCommandHandler
    : AizenCommandHandler<UpdateOfferTemplateBffCommand, ProviderOfferTemplateDto>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder _h;
    private readonly IServiceRequestRemoteCall _sr;

    public UpdateOfferTemplateBffCommandHandler(IProviderProfileResolver r, IProviderIdentityHolder h, IServiceRequestRemoteCall sr)
    { _resolver = r; _h = h; _sr = sr; }

    public override async Task<ProviderOfferTemplateDto?> Handle(UpdateOfferTemplateBffCommand cmd, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        return (await _sr.UpdateTemplate(cmd.Id, cmd.Body)).Body;
    }
}

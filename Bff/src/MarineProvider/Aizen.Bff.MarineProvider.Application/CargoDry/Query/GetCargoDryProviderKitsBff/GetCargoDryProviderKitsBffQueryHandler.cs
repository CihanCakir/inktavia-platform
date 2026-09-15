using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

[DocumentationInfo("Get CargoDry provider kits BFF query handler",
    "Resolves the provider identity, then passes through the module provider kit picker. CargoDry supply v2.")]
public sealed class GetCargoDryProviderKitsBffQueryHandler
    : AizenQueryHandler<GetCargoDryProviderKitsBffQuery, List<CargoDryProviderKitDto>>
{
    private readonly IProviderProfileResolver _resolver;
    private readonly IProviderIdentityHolder  _holder;
    private readonly ICargoDryRemoteCall       _remote;

    public GetCargoDryProviderKitsBffQueryHandler(
        IProviderProfileResolver resolver, IProviderIdentityHolder holder, ICargoDryRemoteCall remote)
    {
        _resolver = resolver;
        _holder   = holder;
        _remote   = remote;
    }

    public override async Task<List<CargoDryProviderKitDto>?> Handle(
        GetCargoDryProviderKitsBffQuery request, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_holder.ProfileId is null or 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        return (await _remote.GetKits(request.ProductCode, request.Status, request.PageSize)).Body;
    }
}

using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProviderPlanById;

[DocumentationInfo("Get provider plan by id BFF query handler",
    "Fetches a single provider plan by its primary key from the Payment module.")]
public sealed class GetProviderPlanByIdBffQueryHandler
    : AizenQueryHandler<GetProviderPlanByIdBffQuery, ProviderPlanBffDto?>
{
    private readonly IPaymentRemoteCall _remote;

    public GetProviderPlanByIdBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ProviderPlanBffDto?> Handle(
        GetProviderPlanByIdBffQuery request, CancellationToken ct)
        => await _remote.GetProviderPlanByIdAsync(request.Id, ct);
}

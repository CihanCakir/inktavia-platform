using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetParticipantPlanById;

[DocumentationInfo("Get participant plan by id BFF query handler",
    "Fetches a single participant plan by its primary key from the Payment module.")]
public sealed class GetParticipantPlanByIdBffQueryHandler
    : AizenQueryHandler<GetParticipantPlanByIdBffQuery, ParticipantPlanBffDto?>
{
    private readonly IPaymentRemoteCall _remote;

    public GetParticipantPlanByIdBffQueryHandler(IPaymentRemoteCall remote)
        => _remote = remote;

    public override async Task<ParticipantPlanBffDto?> Handle(
        GetParticipantPlanByIdBffQuery request, CancellationToken ct)
        => await _remote.GetParticipantPlanByIdAsync(request.Id, ct);
}

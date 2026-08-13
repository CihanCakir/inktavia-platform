using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Approve vessel document command handler", "Approves a vessel document (sets ApprovedAt/ApprovedByUserId) via the Vessel module. Idempotent.")]
public sealed class ApproveVesselDocumentBffCommandHandler
    : AizenCommandHandler<ApproveVesselDocumentBffCommand, ApproveVesselDocumentResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public ApproveVesselDocumentBffCommandHandler(IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<ApproveVesselDocumentResponse?> Handle(
        ApproveVesselDocumentBffCommand request, CancellationToken cancellationToken)
    {
        var result = await _vessel.ApproveVesselDocument(request.VesselId, request.DocumentId);
        return result.Body;
    }
}

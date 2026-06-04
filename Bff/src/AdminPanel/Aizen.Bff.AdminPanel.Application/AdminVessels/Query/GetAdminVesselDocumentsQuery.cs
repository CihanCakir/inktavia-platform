using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselDocumentsQuery : AizenQuery<AdminVesselDocumentsResponse>
{
    public long VesselId { get; }
    public string Authorization { get; }
    public GetAdminVesselDocumentsQuery(long vesselId, string authorization)
    {
        VesselId = vesselId;
        Authorization = authorization;
    }
}

[DocumentationInfo("Get admin vessel documents query handler", "Fetches vessel detail, documents and owners in parallel for the admin vessel detail screen.")]
public sealed class GetAdminVesselDocumentsQueryHandler
    : AizenQueryHandler<GetAdminVesselDocumentsQuery, AdminVesselDocumentsResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public GetAdminVesselDocumentsQueryHandler(IVesselAdminBffRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<AdminVesselDocumentsResponse?> Handle(
        GetAdminVesselDocumentsQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselDocumentsResponse();

        var detailTask = _vessel.GetVesselById(request.VesselId, request.Authorization);
        var docsTask = _vessel.GetVesselDocuments(request.VesselId, request.Authorization);
        var ownersTask = _vessel.GetVesselOwners(request.VesselId, request.Authorization);

        await Task.WhenAll(
            detailTask.ContinueWith(_ => { }),
            docsTask.ContinueWith(_ => { }),
            ownersTask.ContinueWith(_ => { }));

        if (detailTask.IsCompletedSuccessfully)
            response.Vessel = detailTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Detail", "Could not retrieve vessel detail."));

        if (docsTask.IsCompletedSuccessfully)
            response.Documents = docsTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Documents", "Could not retrieve vessel documents."));

        if (ownersTask.IsCompletedSuccessfully)
            response.Owners = ownersTask.Result.Body;
        else
            response.Warnings.Add(AdminBffWarning.CallFailed("Vessel.Owners", "Could not retrieve vessel owners."));

        return response;
    }
}

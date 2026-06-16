using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup group command", "Carries the request payload and caller token for creating a new lookup group via the ReferenceData admin endpoint.")]
public sealed record CreateReferenceDataLookupGroupCommand(
    CreateLookupGroupRequest Request,
    string UserToken);

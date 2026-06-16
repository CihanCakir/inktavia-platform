using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;

namespace Aizen.Bff.AdminPanel.Application.AdminReferenceData.Command;

[DocumentationInfo("Create lookup item command", "Carries the request payload and caller token for creating a new lookup item via the ReferenceData admin endpoint.")]
public sealed record CreateReferenceDataLookupItemCommand(
    CreateLookupItemRequest Request,
    string UserToken);

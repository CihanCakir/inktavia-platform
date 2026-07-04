using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.LookupCargoDryKitAdminBff;

public sealed class LookupCargoDryKitAdminBffQuery : AizenQuery<LookupCargoDryKitAdminBffResponse>
{
    public string Query { get; init; } = default!;
}

public sealed class LookupCargoDryKitAdminBffResponse
{
    public CargoDryKitLookupResultBffDto? Result { get; init; }
}

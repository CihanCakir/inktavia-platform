using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.LookupCargoDryKitAdminBff;

public sealed class LookupCargoDryKitAdminBffQuery : AizenQuery<LookupCargoDryKitAdminBffResponse>
{
    public string Query { get; init; } = default!;
}

public sealed class LookupCargoDryKitAdminBffResponse
{
    public CargoDryKitLookupResultBffDto? Result { get; init; }
}

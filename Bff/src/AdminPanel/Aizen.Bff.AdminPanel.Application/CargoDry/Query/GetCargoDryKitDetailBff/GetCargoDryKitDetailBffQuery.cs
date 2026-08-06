using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitDetailBff;

public sealed class GetCargoDryKitDetailBffQuery : AizenQuery<GetCargoDryKitDetailBffResponse>
{
    public long KitId { get; init; }
}

public sealed class GetCargoDryKitDetailBffResponse
{
    public CargoDryKitDetailBffDto? Kit { get; init; }
}

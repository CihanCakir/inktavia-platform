using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryInventoryDetail;

public sealed class GetCargoDryInventoryDetailBffQuery : AizenQuery<GetCargoDryInventoryDetailBffResponse>
{
    public long ProviderProfileId { get; init; }
}

public sealed class GetCargoDryInventoryDetailBffResponse
{
    public CargoDryProviderInventoryDetailBffDto? Detail { get; init; }
}

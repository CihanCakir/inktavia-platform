using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryAllocationPreview;

public sealed class GetCargoDryAllocationPreviewBffQuery : AizenQuery<GetCargoDryAllocationPreviewBffResponse>
{
    public string BatchCode         { get; init; } = default!;
    public long   ProviderProfileId { get; init; }
    public int    CommercialModel   { get; init; }
}

public sealed class GetCargoDryAllocationPreviewBffResponse
{
    public BatchAllocationPreviewBffDto? Preview { get; init; }
}

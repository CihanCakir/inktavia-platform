using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchByCode;

public sealed class GetCargoDryBatchByCodeBffQuery : AizenQuery<GetCargoDryBatchByCodeBffResponse>
{
    public string BatchCode { get; init; } = default!;
}

public sealed class GetCargoDryBatchByCodeBffResponse
{
    public CargoDryBatchBffDto? Batch { get; init; }
}

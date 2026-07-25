using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDrySettlementsBffQuery : AizenQuery<CargoDryProviderSettlementPagedResultDto>
{
    public int? Status   { get; init; }
    public int  Page     { get; init; } = 1;
    public int  PageSize { get; init; } = 20;
}

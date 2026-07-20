using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryTrendBffQuery : AizenQuery<List<CargoDryEarningsTrendPointDto>>
{
    public int Months { get; init; } = 6;
}

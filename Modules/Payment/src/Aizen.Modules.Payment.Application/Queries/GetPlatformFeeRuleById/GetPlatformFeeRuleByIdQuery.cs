using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetPlatformFeeRuleById;

public sealed class GetPlatformFeeRuleByIdQuery : AizenQuery<PlatformFeeRuleDto>
{
    public long Id { get; init; }
}

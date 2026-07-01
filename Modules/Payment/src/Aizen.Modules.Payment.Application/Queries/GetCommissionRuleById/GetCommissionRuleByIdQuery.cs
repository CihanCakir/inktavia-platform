using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetCommissionRuleById;

public sealed class GetCommissionRuleByIdQuery : AizenQuery<CommissionRuleDto>
{
    public long Id { get; init; }
}

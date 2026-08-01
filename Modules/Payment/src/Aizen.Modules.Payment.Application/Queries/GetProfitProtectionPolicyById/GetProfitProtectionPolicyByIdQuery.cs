using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProfitProtectionPolicyById;

public sealed class GetProfitProtectionPolicyByIdQuery : AizenQuery<ProfitProtectionPolicyDto>
{
    public long Id { get; init; }
}

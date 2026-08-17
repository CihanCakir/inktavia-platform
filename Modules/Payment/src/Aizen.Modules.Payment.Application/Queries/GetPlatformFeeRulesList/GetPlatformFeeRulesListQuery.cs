using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetPlatformFeeRulesList;

public sealed class GetPlatformFeeRulesListQuery : AizenQuery<PlatformFeeRuleListResult>
{
    public PlatformFeeModel?     Model        { get; init; }
    public CommissionRuleStatus? Status       { get; init; }
    public string?               CurrencyCode { get; init; }
    public string?               CategoryCode { get; init; }
    public string?               CustomerType { get; init; }
    public int                   Page         { get; init; } = 1;
    public int                   PageSize     { get; init; } = 20;
}

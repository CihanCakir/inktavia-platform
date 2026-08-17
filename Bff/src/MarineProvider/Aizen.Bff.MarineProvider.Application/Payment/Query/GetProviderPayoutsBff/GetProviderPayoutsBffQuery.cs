using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderPayoutsBffQuery : AizenQuery<ProviderPayoutPagedResultDto>
{
    public int? Status   { get; init; }
    public int  Page     { get; init; } = 1;
    public int  PageSize { get; init; } = 20;
}

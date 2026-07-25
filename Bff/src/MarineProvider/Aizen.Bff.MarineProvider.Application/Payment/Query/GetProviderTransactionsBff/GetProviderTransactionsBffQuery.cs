using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderTransactionsBffQuery : AizenQuery<ProviderTransactionPagedResultDto>
{
    public int? Status   { get; init; }
    public int? Type     { get; init; }
    public int  Page     { get; init; } = 1;
    public int  PageSize { get; init; } = 20;
}

using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderInvoicesBffQuery : AizenQuery<ProviderInvoicePagedResultDto>
{
    public int? Status   { get; init; }
    public int? Type     { get; init; }
    public int  Page     { get; init; } = 1;
    public int  PageSize { get; init; } = 20;
}

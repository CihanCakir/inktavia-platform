using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderInvoiceByIdBffQuery : AizenQuery<ProviderInvoiceDetailDto>
{
    public long InvoiceId { get; init; }
}

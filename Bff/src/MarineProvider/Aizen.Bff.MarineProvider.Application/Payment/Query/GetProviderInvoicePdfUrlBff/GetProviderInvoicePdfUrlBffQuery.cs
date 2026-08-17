using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderInvoicePdfUrlBffQuery : AizenQuery<ProviderFilePdfUrlDto>
{
    public long InvoiceId { get; init; }
}

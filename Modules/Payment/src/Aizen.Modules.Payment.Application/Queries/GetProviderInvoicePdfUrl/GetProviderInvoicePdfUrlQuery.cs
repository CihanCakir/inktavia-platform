using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderInvoicePdfUrl;

public sealed class GetProviderInvoicePdfUrlQuery : AizenQuery<ProviderFilePdfUrlDto>
{
    public long ProviderProfileId { get; init; }
    public long InvoiceId         { get; init; }
}

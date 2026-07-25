using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderInvoiceById;

public sealed class GetProviderInvoiceByIdQuery : AizenQuery<ProviderInvoiceDetailDto>
{
    public long ProviderProfileId { get; init; }
    public long InvoiceId         { get; init; }
}

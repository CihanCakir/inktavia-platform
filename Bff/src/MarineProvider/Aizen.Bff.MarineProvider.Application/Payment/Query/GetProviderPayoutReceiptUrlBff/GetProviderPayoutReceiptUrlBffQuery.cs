using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.Payment;

public sealed class GetProviderPayoutReceiptUrlBffQuery : AizenQuery<ProviderFilePdfUrlDto>
{
    public long PayoutId { get; init; }
}

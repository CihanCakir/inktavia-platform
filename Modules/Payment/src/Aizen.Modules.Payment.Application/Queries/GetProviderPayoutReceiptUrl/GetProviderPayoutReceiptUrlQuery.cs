using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderPayoutReceiptUrl;

public sealed class GetProviderPayoutReceiptUrlQuery : AizenQuery<ProviderFilePdfUrlDto>
{
    public long ProviderProfileId { get; init; }
    public long PayoutId          { get; init; }
}

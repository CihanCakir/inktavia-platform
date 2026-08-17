using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Offers;

public sealed class WithdrawOfferBffCommand : AizenCommand<WithdrawOfferBffResponse>
{
    public long ServiceRequestId { get; init; }
    public long OfferId { get; init; }
    public string? Reason { get; init; }
}

public sealed class WithdrawOfferBffResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

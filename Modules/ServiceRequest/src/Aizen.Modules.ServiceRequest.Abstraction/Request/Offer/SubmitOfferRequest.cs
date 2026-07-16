namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;

public sealed class SubmitOfferRequest
{
    /// <summary>Client-generated idempotency key. Same key = same result, not a second offer.</summary>
    public string IdempotencyKey { get; set; } = default!;
}

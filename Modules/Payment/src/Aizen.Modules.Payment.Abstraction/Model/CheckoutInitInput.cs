namespace Aizen.Modules.Payment.Abstraction.Model
{
    public sealed class CheckoutInitInput
    {
        public required long TransactionId { get; init; }
        public required decimal Amount { get; init; }
        public required string Currency { get; init; }        // "TRY"
        public required string Description { get; init; }     // "Activity #123 ticket"
        public string? IdempotencyKey { get; init; }          // act:..:prof:..:sch:..
        public string? SubMerchantAccountId { get; init; }    // organizer'ın ProviderAccountId (marketplace)
        public string? ReturnUrl { get; init; }               // success URL
        public string? CancelUrl { get; init; }               // cancel URL

    }
}
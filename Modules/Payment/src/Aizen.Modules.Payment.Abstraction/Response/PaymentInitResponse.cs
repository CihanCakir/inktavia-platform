namespace Aizen.Modules.Payment.Abstraction.Response
{
    public sealed class PaymentInitResponse
    {
        public required string Provider { get; init; }
        public required string TransactionReference { get; init; } // checkout/session id
        public required string CheckoutUrl { get; init; }
        public string? ClientSecret { get; init; }
    }
}
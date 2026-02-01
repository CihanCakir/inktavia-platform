namespace Aizen.Modules.Payment.Abstraction.Model
{
    public class CheckoutInitResult
    {
        public required string Provider { get; init; }                // "iyzico"
        public required string TransactionReference { get; init; }    // checkout/session id
        public required string CheckoutUrl { get; init; }             // yönlendirme
        public string? ClientSecret { get; init; }
    }
}
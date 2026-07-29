namespace Aizen.Modules.Payment.Abstraction.Model;

public sealed class ReleaseEscrowInput
{
    public required long   TransactionId      { get; init; }
    public required string GatewayReference   { get; init; }  // Iyzico checkoutToken / manual ref
    /// <summary>BE-P9-fix §4: the CF-retrieve item paymentTransactionId (approve target). Null → cannot release via iyzico.</summary>
    public string? GatewayItemTransactionId   { get; init; }
    public required decimal ProviderNetAmount  { get; init; }  // Amount to transfer to provider
    public string? SubMerchantKey              { get; init; }  // Iyzico sub-merchant key (null for manual)
    public string? AdminNote                   { get; init; }  // Manual provider note
}

namespace Aizen.Modules.Payment.Abstraction.Model;

public sealed class ReleaseEscrowInput
{
    public required long   TransactionId      { get; init; }
    public required string GatewayReference   { get; init; }  // Iyzico checkoutToken / manual ref
    public required decimal ProviderNetAmount  { get; init; }  // Amount to transfer to provider
    public string? SubMerchantKey              { get; init; }  // Iyzico sub-merchant key (null for manual)
    public string? AdminNote                   { get; init; }  // Manual provider note
}

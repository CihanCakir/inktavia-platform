using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

public sealed class CargoDryKitRenewedMessage : AizenBaseMessage
{
    public long           KitId             { get; set; }
    public string         KitCode           { get; set; } = default!;
    public long           OwnerUserId       { get; set; }
    public DateTimeOffset NewExpiresAt      { get; set; }
    public string         RenewalType       { get; set; } = default!;

    /// <summary>
    /// Renewal payment amount in TRY. Zero for free/complimentary renewals — Payment module
    /// will skip transaction creation when this value is zero.
    /// </summary>
    public decimal        RenewalAmountTRY  { get; set; }
    public string         CurrencyCode      { get; set; } = "TRY";
}

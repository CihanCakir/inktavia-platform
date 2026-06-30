namespace Aizen.Modules.Payment.Abstraction.Enum;

public enum InkCoinDirection
{
    Earn    = 1,  // Coins credited
    Spend   = 2,  // Coins debited for a purchase/discount
    Expire  = 3,  // Coins expired after TTL
    Adjust  = 4,  // Admin manual correction (with note)
    Reverse = 5,  // Reversal of a prior Earn (e.g. on refund/cancellation)
}

namespace Aizen.Modules.Payment.Abstraction.Extension
{
    public static class PaymentEnumExtension
    {
        public static PurchaseType ToPurchaseType(this TransactionType transactionType)
        {
            return transactionType switch
            {
                TransactionType.ActivityParticipation => PurchaseType.Activity,
                TransactionType.VenueReservation => PurchaseType.Venue,
                TransactionType.Subscription => PurchaseType.Package,
                TransactionType.FeaturePurchase => PurchaseType.Feature,
                _ => throw new ArgumentOutOfRangeException(nameof(transactionType))
            };
        }
    }
}
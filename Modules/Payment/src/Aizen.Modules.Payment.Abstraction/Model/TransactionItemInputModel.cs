namespace Aizen.Modules.Payment.Abstraction
{
    public class TransactionItemInputModel
    {
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsCommission { get; set; }
        public bool IsVat { get; set; }
    }

}
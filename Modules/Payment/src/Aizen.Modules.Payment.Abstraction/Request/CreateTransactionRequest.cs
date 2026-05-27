using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Model;

namespace Aizen.Modules.Payment.Abstraction
{
    public class CreateTransactionRequest
    {
        public long UserProfileId { get; set; }
        public TransactionType TransactionType { get; set; }
        public decimal TotalAmount { get; set; }
        public required List<CreateTransactionItemRequest> Items { get; set; }

        // Yeni: bağlamı da taşıyabilsin
        public required TransactionContext Context { get; set; }
        public required string Currency { get; set; }
        public required string Description { get; set; }
        public string? IdempotencyKey { get; set; }
        public string? SubMerchantAccountId { get; set; }
    }

    public class CreateTransactionItemRequest
    {
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsCommission { get; set; }
        public bool IsVat { get; set; }
    }
}

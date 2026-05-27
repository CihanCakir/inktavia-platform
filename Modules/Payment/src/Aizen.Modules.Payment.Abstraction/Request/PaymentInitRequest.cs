using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Payment.Abstraction.Model;

namespace Aizen.Modules.Payment.Abstraction.Request
{
    public sealed class PaymentInitRequest
    {
        public required long UserProfileId { get; init; }
        public required TransactionType TransactionType { get; init; }
        public required decimal TotalAmount { get; init; }
        public required List<CreateTransactionItemRequest> Items { get; init; }

        public required TransactionContext Context { get; init; }

        // Provider init bilgileri
        public required string Currency { get; init; }     // "TRY"
        public required string Description { get; init; }  // "Activity #123 ticket"
        public string? IdempotencyKey { get; init; }       // act:..:prof:..:sch:..
        public string? SubMerchantAccountId { get; init; } // Organizer ProviderAccountId (marketplace)
    }
}
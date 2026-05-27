using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Response
{
    public sealed class PaymentApplyResult
    {
        public bool Processed { get; init; }           // yeni işlendi mi (idempotent değil)
        public bool IsPaid { get; init; }              // event gerçekten ödeme-succeeded mi
        public long? TransactionId { get; init; }
        public string? TransactionReference { get; init; }
    }
}
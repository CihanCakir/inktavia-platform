using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Dto
{
    public sealed class PaymentInitResponseDto
    {
        public required string Provider { get; init; }          // "iyzico" | "stripe" ...
        public required string TransactionReference { get; init; } // provider tarafı referans / checkout session id
        public required string CheckoutUrl { get; init; }       // web yönlendirme için
        public string? ClientSecret { get; init; }              // native/mobile için
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Model
{
   public sealed class PricingResult
    {
        public string Currency { get; init; } = "TRY";
        public decimal SubtotalExclVat { get; init; }  // KDV hariç ara toplam (net taban)
        public decimal VatTotal { get; init; }         // toplam KDV
        public decimal GrandTotal { get; init; }       // ödenecek toplam (kullanıcıya gösterilecek)
        public IReadOnlyList<PricingLine> Lines { get; init; } = Array.Empty<PricingLine>();

        // Paylaşım (opsiyonel, raporlama için)
        public decimal PlatformCommissionExclVat { get; init; }
        public decimal PlatformCommissionVat { get; init; }
        public decimal OrganizerShare { get; init; }   // organizer net (KDV dahil/ hariç modelinize göre set)
        public decimal VenueShare { get; init; }       // venue payı kullanıyorsanız set
    }
}
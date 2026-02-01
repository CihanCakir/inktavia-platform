using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Response
{
    public sealed class PaymentWebhookResponse
    {
        public bool IsPaid { get; set; }
        public bool AlreadyProcessed { get; set; }

        public long? TransactionId { get; set; }
        public string? ProviderReference { get; set; }

        public long? ActivityId { get; set; }
        public long? ScheduleId { get; set; }
        public long? UserProfileId { get; set; }

        public string? QrHash { get; set; }

        // UI buton için
        public string? CtaText { get; set; }   // örn: "Bileti Aç"
        public string? CtaUrl { get; set; }    // örn: $"/tickets/{QrHash}" veya $"/activities/{ActivityId}/ticket?qr={QrHash}"

        public string? Message { get; set; }   // örn: "Ödeme alındı ve bilet hazır."
    }

}
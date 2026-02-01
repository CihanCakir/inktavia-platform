using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Model
{
    public enum PricingSubjectType { ActivityTicket, Subscription, OrganizerFeature, VenueAddOn }

    public sealed class PricingLine
    {
        public string Code { get; init; } = default!;  // e.g. "TICKET", "SERVICE_FEE", "VAT_TICKET", "VAT_FEE"
        public string Description { get; init; } = default!;
        public decimal Amount { get; init; }
        public bool IsVat { get; init; }
        public bool IsCommission { get; init; }
        public decimal? VatRate { get; init; }         // %20 => 20m
        public string? Meta { get; init; }             // JSON/serbest bilgi
    }

   

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public class UpdateOrganizerProfileRequest
    {
        public string? OwnerFirstName { get; set; }
        public string? OwnerLastName { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? NationalityId { get; set; }
        public TaxpayerType? TaxpayerType { get; set; }

        public bool? AllowPush { get; set; }
        public bool? AllowSms { get; set; }
        public bool? AllowEmail { get; set; }

        // Provider FIXED business location + default per-km travel rate (distance-based pricing, Phase 1).
        public decimal? BusinessLatitude { get; set; }
        public decimal? BusinessLongitude { get; set; }
        public string? BusinessAddressLabel { get; set; }
        public decimal? RatePerKm { get; set; }
    }
}
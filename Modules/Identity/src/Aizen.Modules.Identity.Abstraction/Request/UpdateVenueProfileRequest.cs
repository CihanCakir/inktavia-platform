
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public class UpdateVenueProfileRequest
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
    }
}
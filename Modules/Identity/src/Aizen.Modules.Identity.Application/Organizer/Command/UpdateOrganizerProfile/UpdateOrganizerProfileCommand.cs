using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.UpdateOrganizerProfile
{
    public class UpdateOrganizerProfileCommand : AizenCommand<ProfileUpdateResult>
    {
        public string? OwnerFirstName { get; }
        public string? OwnerLastName { get; }
        public string? Bio { get; }
        public string? ProfilePhotoUrl { get; }
        public string? NationalityId { get; }
        public TaxpayerType? TaxpayerType { get; }
        public bool? AllowPush { get; }
        public bool? AllowSms { get; }
        public bool? AllowEmail { get; }
        public decimal? BusinessLatitude { get; }
        public decimal? BusinessLongitude { get; }
        public string? BusinessAddressLabel { get; }
        public decimal? RatePerKm { get; }

        public UpdateOrganizerProfileCommand(
            string? ownerFirstName,
            string? ownerLastName,
            string? bio,
            string? profilePhotoUrl,
            string? nationalityId,
            TaxpayerType? taxpayerType,
            bool? allowPush,
            bool? allowSms,
            bool? allowEmail,
            decimal? businessLatitude = null,
            decimal? businessLongitude = null,
            string? businessAddressLabel = null,
            decimal? ratePerKm = null)
        {
            OwnerFirstName = ownerFirstName;
            OwnerLastName = ownerLastName;
            Bio = bio;
            ProfilePhotoUrl = profilePhotoUrl;
            NationalityId = nationalityId;
            TaxpayerType = taxpayerType;
            AllowPush = allowPush;
            AllowSms = allowSms;
            AllowEmail = allowEmail;
            BusinessLatitude = businessLatitude;
            BusinessLongitude = businessLongitude;
            BusinessAddressLabel = businessAddressLabel;
            RatePerKm = ratePerKm;
        }
    }
}
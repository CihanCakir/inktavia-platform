using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Model;
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

        public UpdateOrganizerProfileCommand(
            string? ownerFirstName,
            string? ownerLastName,
            string? bio,
            string? profilePhotoUrl,
            string? nationalityId,
            TaxpayerType? taxpayerType,
            bool? allowPush,
            bool? allowSms,
            bool? allowEmail)
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
        }
    }
}
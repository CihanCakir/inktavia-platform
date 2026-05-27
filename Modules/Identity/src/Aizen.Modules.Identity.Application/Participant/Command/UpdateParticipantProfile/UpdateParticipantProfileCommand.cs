using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Model;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class UpdateParticipantProfileCommand : AizenCommand<ProfileUpdateResult>
    {
        public string? FirstName { get; }
        public string? LastName { get; }
        public string? Gender { get; }
        public DateTime? BirthDate { get; }
        public string? Bio { get; }
        public string? ProfilePhotoUrl { get; }
        public string? NationalityId { get; }
        public bool? AllowPush { get; }
        public bool? AllowSms { get; }
        public bool? AllowEmail { get; }

        public UpdateParticipantProfileCommand(
            string? firstName,
            string? lastName,
            string? gender,
            DateTime? birthDate,
            string? bio,
            string? profilePhotoUrl,
            string? nationalityId,
            bool? allowPush,
            bool? allowSms,
            bool? allowEmail)
        {
            FirstName = firstName;
            LastName = lastName;
            Gender = gender;
            BirthDate = birthDate;
            Bio = bio;
            ProfilePhotoUrl = profilePhotoUrl;
            NationalityId = nationalityId;
            AllowPush = allowPush;
            AllowSms = allowSms;
            AllowEmail = allowEmail;
        }
    }
}
    
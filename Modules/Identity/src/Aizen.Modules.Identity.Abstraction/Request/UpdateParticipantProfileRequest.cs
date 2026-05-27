using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public class UpdateParticipantProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? NationalityId { get; set; }

        public bool? AllowPush { get; set; }
        public bool? AllowSms { get; set; }
        public bool? AllowEmail { get; set; }
    }
}
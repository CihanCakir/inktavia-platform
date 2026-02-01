using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction.Enum;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserValidationEntity : AizenEntityWithAudit
    {
        public UserValidationType UserValidationTypeId { get; set; }
        public UserValidationMethodType UserValidationMethodTypeId { get; set; }
        public string ValidationReferance { get; set; } = null!;
        public string? RelationCode { get; set; }
        public int ValdationCode { get; set; }
        public int State { get; set; }
        public int ApplicationId { get; set; }
        public string ValidationGuid { get; set; } = null!;
        public DateTime ExpiredDateTime { get; set; }

        public long UserProfileId { get; set; }
        public virtual UserProfileEntity UserProfile { get; set; } = null!;
        protected UserValidationEntity()
        {
        }

        // ✅ Sadece tek bir versiyon bırakıldı
        public static UserValidationEntity Create(
            UserValidationType validationType,
            UserValidationMethodType methodType,
            string reference,
            int code,
            string guid,
            DateTime expireDate,
            int applicationId,
            long userProfileId,
            string? relationCode = null)
        {
            return new UserValidationEntity
            {
                UserValidationTypeId = validationType,
                UserValidationMethodTypeId = methodType,
                ValidationReferance = reference,
                ValdationCode = code,
                ValidationGuid = guid,
                ExpiredDateTime = expireDate,
                ApplicationId = applicationId,
                State = 1,
                RelationCode = relationCode,
                UserProfileId = userProfileId
            };
        }

        public void MarkAsUsed() => State = 2;

        public void Expire() => State = 3;

        public bool IsExpired() => ExpiredDateTime < DateTime.UtcNow;

        public bool IsValid(int otpCode, string guid)
        {
            return State == 1 && !IsExpired() && ValdationCode == otpCode && ValidationGuid == guid;
        }
    }

}
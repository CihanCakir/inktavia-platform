using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserEmailConfirmEntity : AizenEntityWithAudit
    {
        public long UserId { get; set; }
        public virtual UserEntity User { get; set; } = null!;

        public string Email { get; set; } = null!;
        public bool IsConfirmed { get; set; } = false;

        public string ValidationGuid { get; set; } = null!;
        public string ValidationString { get; set; } = null!;

        public DateTime ExpirationTime { get; set; }

        public static UserEmailConfirmEntity Create(
            long userId,
            string email,
            bool isConfirm,
            string validationGuid,
            string validationString,
            DateTime expirationTime)
        {
            return new UserEmailConfirmEntity
            {
                UserId = userId,
                Email = email,
                IsConfirmed = isConfirm,
                ValidationGuid = validationGuid,
                ValidationString = validationString,
                ExpirationTime = expirationTime
            };
        }
    }
}


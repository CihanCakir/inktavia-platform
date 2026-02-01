using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserPasswordHistoryEntity : AizenEntityWithAudit
    {
        public long UserId { get; set; }
        public required virtual UserEntity User { get; set; }

        public string PasswordHash { get; set; } = null!;
        public bool IsValid { get; set; } = true; // Şu anki aktif parola mı?

        public long? ActiveProfileId { get; set; } // Bağlamda kullanılan profil ID
        public virtual UserProfileEntity? ActiveProfile { get; set; } // Bağlamda kullanılan profil, eğer varsa
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class RoleEntity : IdentityRole<long>, IAizenEntity
    {

        public RoleEntity()
        {
            CreatedAt = DateTime.UtcNow;
            UserRoles = new List<UserRoleEntity>();
        }
        // Ekstra açıklama veya sistem içi kullanım için
        public string? Description { get; set; }

        // RoleType ile ilişki
        public long RoleTypeId { get; set; }
        public virtual RoleTypeEntity? RoleType { get; set; }

        // Kullanıcılara atanan roller
        public virtual ICollection<UserRoleEntity> UserRoles { get; set; }

        // Audit ve soft delete
        public required DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
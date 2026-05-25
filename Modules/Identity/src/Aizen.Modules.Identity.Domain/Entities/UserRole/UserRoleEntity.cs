using Aizen.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserRoleEntity : IdentityUserRole<long>, IAizenEntity
    {
        public UserRoleEntity()
        {
        }

        public virtual UserEntity? User { get; set; }
        public virtual RoleEntity? Role { get; set; }
    }
}
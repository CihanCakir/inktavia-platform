using Aizen.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserRoleEntity : IdentityUserRole<long>, IAizenEntity
    {
        public UserRoleEntity()
        {
        }
        public new long UserId { get; set; }
        public virtual UserEntity? User { get; set; }

        public new long RoleId { get; set; }
        public virtual RoleEntity? Role { get; set; }
    }
}
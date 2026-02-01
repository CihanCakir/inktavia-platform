using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities
{
public class RoleTypeEntity : AizenEntityWithAudit
{
    public string Name { get; set; } = null!; // Example: System, Workshop, Support

    public string? Description { get; set; }  // What is the purpose of grouping this role type?

    public virtual ICollection<RoleEntity> Roles { get; set; } = [];
}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;

namespace Aizen.Modules.Identity.Domain.Extension
{
public static class UserEntityExternalFactory
{
       public static UserEntity CreateExternal(string email, LoginType loginType, bool emailVerified)
        => UserEntity.CreateExternal(email, loginType, emailVerified);
}

}
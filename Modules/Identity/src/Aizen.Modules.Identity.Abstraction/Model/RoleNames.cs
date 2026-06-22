using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Model
{
    public static class RoleNames
    {
        public const string Consumer = "Consumer";
        public const string Organizer = "Organizer";
        public const string Venue = "Venue";
        public const string Moderator = "Moderator";
        public const string Admin = "Admin";

        // Keycloak service-account role assigned to BFF clients via resource_access.identity-api.roles.
        // Used to allow BFF server-to-server calls without requiring the human Admin realm role.
        public const string IdentityAdmin = "identity.admin";
    }
}
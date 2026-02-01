using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Modules.Identity.Abstraction.Enum;

namespace Aizen.Modules.Identity.Abstraction.Dto
{
    public sealed record RegisterResult(
        bool Success,
        RegistrationStatus Status,             // "Active" | "PendingApproval"
        long UserId,
        long? ActiveProfileId,
        string? AccessToken,       // Consumer’da dolu, Organizer/Venue’da null (onaya kadar)
        string? RefreshToken,
        string? Message
    );

}
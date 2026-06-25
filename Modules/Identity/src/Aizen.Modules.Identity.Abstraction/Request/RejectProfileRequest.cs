using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public sealed class RejectProfileRequest
    {
        public string Reason { get; set; } = default!;
        public string? ReasonCategory { get; set; }
        public string? InternalNote { get; set; }
        public bool NotifyUser { get; set; } = true;
    }
}
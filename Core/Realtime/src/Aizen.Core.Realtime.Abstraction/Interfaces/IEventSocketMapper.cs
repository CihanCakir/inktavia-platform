using System.Collections.Generic;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface IEventSocketMapper
    {
        RealtimeMessage? Map(object domainEvent);
        (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent);
    }
}
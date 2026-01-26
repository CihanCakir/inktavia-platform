using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface IUserInfoResolver
    {
        Task<IEnumerable<string>> ResolveUserIdsForEntityAsync(string entityType, string entityId, CancellationToken ct = default);
    }
}
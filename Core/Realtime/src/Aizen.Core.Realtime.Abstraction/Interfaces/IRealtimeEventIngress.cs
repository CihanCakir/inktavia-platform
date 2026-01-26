using System.Threading;
using System.Threading.Tasks;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface IRealtimeEventIngress
    {
        Task PublishDomainEventAsync(EventDto evt, CancellationToken ct = default);
    }
}
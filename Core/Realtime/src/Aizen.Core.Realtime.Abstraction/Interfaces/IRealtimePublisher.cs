using System.Threading;
using System.Threading.Tasks;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface IRealtimePublisher
    {
        Task PublishToChannelAsync(string channel, object payload, string? correlationId = null, CancellationToken ct = default);
        Task PublishToGroupAsync(string groupName, object payload, string? correlationId = null, CancellationToken ct = default);
        Task PublishToUserAsync(string userId, object payload, string? correlationId = null, CancellationToken ct = default);
    }
}
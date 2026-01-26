using System.Threading;
using System.Threading.Tasks;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface IActivityAuthorizationService
    {
        Task<bool> CanJoinAsParticipantAsync(long activityId, string userId, CancellationToken ct = default);
        Task<bool> CanJoinAsOrganizerAsync(long activityId, string userId, CancellationToken ct = default);
        Task<bool> CanSendChatAsync(long activityId, string userId, CancellationToken ct = default);
    }
}
using System.Threading.Tasks;

namespace Aizen.Core.Realtime.Abstraction.Interfaces
{
    public interface IRateLimitGuard
    {
        Task<bool> CheckRateLimitAsync(string connectionId);
        bool IsMessageLengthAllowed(string message);
    }
}
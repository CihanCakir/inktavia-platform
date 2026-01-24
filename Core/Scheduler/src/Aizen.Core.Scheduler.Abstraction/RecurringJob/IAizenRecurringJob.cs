using System.Threading;
using System.Threading.Tasks;

namespace Aizen.Core.Scheduler.Abstraction.RecurringJob
{
    public interface IAizenRecurringJob
    {
        public bool IsActive { get; }

        string CronExpression { get; }

        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}

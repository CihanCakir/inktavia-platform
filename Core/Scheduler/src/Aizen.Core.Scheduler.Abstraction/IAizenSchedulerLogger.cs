using Hangfire.Server;

namespace Aizen.Core.Scheduler.Abstraction
{
    public interface IAizenSchedulerLogger
    {
        void SetPerformContext(PerformContext context);

        void WriteConsole(string message);

        void SetProgressBar(int value);
    }
}

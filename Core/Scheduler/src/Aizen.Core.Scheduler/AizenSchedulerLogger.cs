using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;
using Aizen.Core.Scheduler.Abstraction;

namespace Aizen.Core.Scheduler
{
    public class AizenSchedulerLogger : IAizenSchedulerLogger
    {
        private PerformContext _performContext;
        private IProgressBar _progressBar;

        public void SetPerformContext(PerformContext context)
        {
            this._performContext = context;
            this._performContext.WriteLine();
            this._progressBar = context.WriteProgressBar(0);
        }

        public void WriteConsole(string message)
        {
            this._performContext?.WriteLine(message);
        }

        public void SetProgressBar(int value)
        {
            this._progressBar.SetValue(value);
        }
    }
}

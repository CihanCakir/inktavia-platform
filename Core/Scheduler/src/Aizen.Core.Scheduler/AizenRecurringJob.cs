using Aizen.Core.Scheduler.Abstraction;
using Aizen.Core.Scheduler.Abstraction.RecurringJob;

namespace Aizen.Core.Scheduler
{
    public abstract class AizenRecurringJob : IAizenRecurringJob
    {
        protected readonly IAizenSchedulerLogger Logger;
        protected readonly IServiceProvider ServiceProvider;

        protected AizenRecurringJob(
            IAizenSchedulerLogger logger, IServiceProvider serviceProvider)
        {
            this.Logger = logger;
            ServiceProvider = serviceProvider;
        }

        public abstract bool IsActive { get; }

        public abstract string CronExpression { get; }

        protected abstract Task ProcessAsync(CancellationToken cancellationToken);

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.IsActive)
            {
                await this.ProcessAsync(cancellationToken);
            }
            else
            {
                this.Logger.WriteConsole($"Recurring job {this.GetType().Name} is not active.");
            }

            this.Logger.SetProgressBar(100);
        }
    }
}

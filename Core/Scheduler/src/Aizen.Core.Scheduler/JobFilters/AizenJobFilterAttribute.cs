using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Console;
using Hangfire.Server;
using Hangfire.States;
using Aizen.Core.Scheduler.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Scheduler.JobFilters
{
    public class AizenJobFilterAttribute : JobFilterAttribute, IClientFilter, IServerFilter, IElectStateFilter
    {
        //private readonly IAizenLogger _aizenLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public AizenJobFilterAttribute(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            this._httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            //var aizenLoggerFactory = serviceProvider.GetRequiredService<IAizenLoggerFactory>();
            //this._aizenLogger = aizenLoggerFactory.CreateLogger(AizenLogType.Scheduler);
            //serviceProvider.GetRequiredService<IAizenSchedulerLogger>();
        }

        public void OnCreating(CreatingContext filterContext)
        {
            //this._aizenLogger.LogInformation($"Job with method ${filterContext.Job.Method.Name} is being created");
        }

        public void OnCreated(CreatedContext filterContext)
        {
            //this._aizenLogger.LogInformation($"Job with method {filterContext.Job.Method.Name} has been created with id {filterContext.BackgroundJob?.Id}");
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            if (this._httpContextAccessor.HttpContext is null)
            {
                this._httpContextAccessor.HttpContext = new DefaultHttpContext
                {
                    RequestServices = this._serviceProvider
                };
            }
            var message = $"Job with id {filterContext.BackgroundJob.Id} is being performed.";
            filterContext.WriteLine(ConsoleTextColor.Green, message);
            //this._aizenLogger.LogInformation(message);
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext.Exception == null)
            {
                var message = $"Job with id {filterContext.BackgroundJob.Id} has been performed.";
                filterContext.WriteLine(ConsoleTextColor.Green, message);
                //this._aizenLogger.LogInformation(message);
            }
            else
            {
                filterContext.WriteLine(ConsoleTextColor.Red, filterContext.Exception.ToString());
            }
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState failedState)
            {
                //this._aizenLogger.LogError($"Job with id {context.BackgroundJob.Id} has been failed", failedState.Exception);
            }
        }
    }
}

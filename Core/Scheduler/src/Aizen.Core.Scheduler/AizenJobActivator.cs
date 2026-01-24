using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Aizen.Core.Common.Abstraction.Enums;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Core.Scheduler.Abstraction.BackgroundJob;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Scheduler
{
    public class AizenJobActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public AizenJobActivator(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public override JobActivatorScope BeginScope(PerformContext context)
        {
            var scope = this._serviceProvider.CreateScope();
            //var aizenLoggerFactory = scope.ServiceProvider.GetRequiredService<IAizenLoggerFactory>();
            //var aizenLogger = aizenLoggerFactory.CreateLogger(AizenLogType.Scheduler);

            try
            {
                var jobData = context.BackgroundJob.Job.Args.Any(x => x is AizenBackgroundJobData);
                if (jobData)
                {
                    var data = (AizenBackgroundJobData)context.BackgroundJob.Job.Args.Single(x => x is AizenBackgroundJobData);

                    scope.ServiceProvider.GetRequiredService<IAizenInfoContainer>().Set(data.ClientInfo);
                    scope.ServiceProvider.GetRequiredService<IAizenInfoContainer>().Set(data.CustomerInfo);
                    scope.ServiceProvider.GetRequiredService<IAizenInfoContainer>().Set(data.DeviceInfo);
                }

                scope.ServiceProvider.GetRequiredService<IAizenInfoContainer>().Set(new AizenExecutionInfo
                {
                    ExecutionId = Guid.NewGuid().ToString(),
                    ExecutionType = ExecutionType.Scheduler,
                    SchedulerInfo = new SchedulerInfo
                    {
                        Id = context.BackgroundJob.Id,
                        CreatedAt = context.BackgroundJob.CreatedAt,
                        TypeName = context.BackgroundJob.Job.Type.Name,
                        MethodName = context.BackgroundJob.Job.Method.Name,
                    }
                });

                scope.ServiceProvider.GetRequiredService<IAizenSchedulerLogger>().SetPerformContext(context);

                //aizenLogger.LogInformation("AizenBackgroundJobActivator_BeginScope", context.BackgroundJob, AizenLogMethod.Response);
                context.WriteLine("Job Activator: Begin scope");
            }
            catch (Exception ex)
            {
                //aizenLogger.LogError(ex.Message, ex);
                context.WriteLine($"Job Activator: Error - {ex.Message}");
            }

            return new AizenBackgroundJobActivatorScope(scope);
        }
    }
}

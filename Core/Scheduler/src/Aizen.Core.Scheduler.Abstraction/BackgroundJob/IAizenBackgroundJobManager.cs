using System;
using Hangfire;
using Aizen.Core.Scheduler.Abstraction.BackgroundJob;

namespace Aizen.Core.Scheduler.Abstraction
{
    public interface IAizenBackgroundJobManager
    {
        string Enqueue<TJob>()
            where TJob : IAizenBackgroundJob<AizenBackgroundJobData>;

        string Enqueue<TJob, TJobData>(TJobData jobData)
            where TJobData : AizenBackgroundJobData
            where TJob : IAizenBackgroundJob<TJobData>;

        string Enqueue<TJob, TJobData>(IBackgroundJobClient backgroundJobClient,  TJobData jobData)
            where TJobData : AizenBackgroundJobData
            where TJob : IAizenBackgroundJob<TJobData>;

        string ContinueJobWith<TJob, TJobData>(IBackgroundJobClient backgroundJobClient, string parentId,
            TJobData jobData)
            where TJobData : AizenBackgroundJobData
            where TJob : IAizenBackgroundJob<TJobData>;

        string Schedule<TJob>(DateTimeOffset enqueueAt)
            where TJob : IAizenBackgroundJob<AizenBackgroundJobData>;

        string Schedule<TJob>(TimeSpan delay)
            where TJob : IAizenBackgroundJob<AizenBackgroundJobData>;

        string Schedule<TJob, TJobData>(TJobData jobData, DateTimeOffset enqueueAt)
            where TJobData : AizenBackgroundJobData
            where TJob : IAizenBackgroundJob<TJobData>;

        string Schedule<TJob, TJobData>(TJobData jobData, TimeSpan delay)
            where TJobData : AizenBackgroundJobData
            where TJob : IAizenBackgroundJob<TJobData>;
    }
}

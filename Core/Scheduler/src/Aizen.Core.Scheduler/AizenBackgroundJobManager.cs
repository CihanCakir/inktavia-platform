using Hangfire;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Scheduler.Abstraction;
using Aizen.Core.Scheduler.Abstraction.BackgroundJob;

namespace Aizen.Core.Scheduler
{
    internal class AizenBackgroundJobManager : IAizenBackgroundJobManager
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IAizenInfoAccessor _aizenInfoAccessor;

        public AizenBackgroundJobManager(
            IBackgroundJobClient backgroundJobClient,
            IAizenInfoAccessor aizenInfoAccessor
           )
        {
            this._backgroundJobClient = backgroundJobClient;
            this._aizenInfoAccessor = aizenInfoAccessor;
        }

        #region Fire And Forget Jobs
        public string Enqueue<TJob>()
            where TJob : IAizenBackgroundJob<AizenBackgroundJobData>
        {
            var jobData = new AizenBackgroundJobData()
            {
                ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo,
                CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo,
                DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo
            };

            return this._backgroundJobClient.Enqueue<TJob>(job => job.Execute(jobData));
        }

        public string Enqueue<TJob, TJobData>(TJobData jobData)
            where TJob : IAizenBackgroundJob<TJobData>
            where TJobData : AizenBackgroundJobData
        {
            jobData.ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo;
            jobData.CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo;
            jobData.DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo;
            
            //this._aizenLogger.LogInformation("AizenScheduler_Enqueue", jobData, AizenLogMethod.Request);
            return this._backgroundJobClient.Enqueue<TJob>(job => job.Execute(jobData));
        }

        public string Enqueue<TJob, TJobData>(IBackgroundJobClient backgroundJobClient,  TJobData jobData)
            where TJob : IAizenBackgroundJob<TJobData>
            where TJobData : AizenBackgroundJobData
        {
            jobData.ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo;
            jobData.CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo;
            jobData.DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo;

            //this._aizenLogger.LogInformation("AizenScheduler_Enqueue", jobData, AizenLogMethod.Request);
            return backgroundJobClient.Enqueue<TJob>(job => job.Execute(jobData));
        }

        public string ContinueJobWith<TJob, TJobData>(IBackgroundJobClient backgroundJobClient, string parentId, TJobData jobData)
            where TJob : IAizenBackgroundJob<TJobData>
            where TJobData : AizenBackgroundJobData
        {
            jobData.ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo;
            jobData.CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo;
            jobData.DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo;

            //this._aizenLogger.LogInformation("AizenScheduler_Enqueue", jobData, AizenLogMethod.Request);
            return backgroundJobClient.ContinueJobWith<TJob>(parentId, job => job.Execute(jobData));
        }
        #endregion

        #region Delayed Jobs
        public string Schedule<TJob>(DateTimeOffset enqueueAt)
            where TJob : IAizenBackgroundJob<AizenBackgroundJobData>
        {
            var jobData = new AizenBackgroundJobData()
            {
                ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo,
                CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo,
                DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo
            };

            //this._aizenLogger.LogInformation("AizenScheduler_Schedule", jobData, AizenLogMethod.Request);
            return this._backgroundJobClient.Schedule<TJob>(job => job.Execute(jobData), enqueueAt);
        }

        public string Schedule<TJob>(TimeSpan delay)
            where TJob : IAizenBackgroundJob<AizenBackgroundJobData>
        {
            var jobData = new AizenBackgroundJobData()
            {
                //ClientInfo = this._aizenInfoAccessor.ClientInfo,
                //CustomerInfo = this._aizenInfoAccessor.CustomerInfo,
                //DeviceInfo = this._aizenInfoAccessor.DeviceInfo
            };

            //this._aizenLogger.LogInformation("AizenScheduler_Schedule", jobData, AizenLogMethod.Request);
            return this._backgroundJobClient.Schedule<TJob>(job => job.Execute(jobData), delay);
        }

        public string Schedule<TJob, TJobData>(TJobData jobData, DateTimeOffset enqueueAt)
            where TJob : IAizenBackgroundJob<TJobData>
            where TJobData : AizenBackgroundJobData
        {
            jobData.ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo;
            jobData.CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo;
            jobData.DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo;

            //this._aizenLogger.LogInformation("AizenScheduler_Schedule", jobData, AizenLogMethod.Request);
            return this._backgroundJobClient.Schedule<TJob>(job => job.Execute(jobData), enqueueAt);
        }

        public string Schedule<TJob, TJobData>(TJobData jobData, TimeSpan delay)
            where TJob : IAizenBackgroundJob<TJobData>
            where TJobData : AizenBackgroundJobData
        {
            jobData.ClientInfo = this._aizenInfoAccessor.ClientInfoAccessor.ClientInfo;
            jobData.CustomerInfo = this._aizenInfoAccessor.UserInfoAccessor.UserInfo;
            jobData.DeviceInfo = this._aizenInfoAccessor.DeviceInfoAccessor.DeviceInfo;

            //this._aizenLogger.LogInformation("AizenScheduler_Schedule", jobData, AizenLogMethod.Request);
            return this._backgroundJobClient.Schedule<TJob>(job => job.Execute(jobData), delay);
        }

        #endregion
    }
}

using System.Threading.Tasks;
using Aizen.Core.Scheduler.Abstraction.BackgroundJob;

namespace Aizen.Core.Scheduler.Abstraction
{
    public interface IAizenBackgroundJob<in TJobData> : IAizenBackgroundJob
        where TJobData : AizenBackgroundJobData
    {
        Task Execute(TJobData jobData);
    }
}

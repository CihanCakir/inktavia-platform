using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.Starter.Scheduler;

public sealed class AizenSchedulerApplication : AizenApplication
{
    public AizenSchedulerApplication(WebApplication application) : base(application)
    {
    }
}
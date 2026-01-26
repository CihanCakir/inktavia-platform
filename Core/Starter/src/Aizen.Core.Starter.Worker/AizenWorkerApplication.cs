using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.Starter.Worker;

public sealed class AizenWorkerApplication : AizenApplication
{
    public AizenWorkerApplication(WebApplication application) : base(application)
    {
    }
}
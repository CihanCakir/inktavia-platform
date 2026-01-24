using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.Starter.Api;

public sealed class AizenApiApplication : AizenApplication
{
    public AizenApiApplication(WebApplication application) : base(application)
    {
    }
}
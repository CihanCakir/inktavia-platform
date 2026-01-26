using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Builder;

namespace Aizen.Core.Starter.Api;

public sealed class AizenBffApplication : AizenApplication
{
    public AizenBffApplication(WebApplication application) : base(application)
    {
    }
}
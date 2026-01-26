using Aizen.Core.Starter.Abstraction;
using Microsoft.AspNetCore.Builder;


namespace Aizen.Core.Starter.Operation;
public class AizenOperationApplication : AizenApplication
{
    public AizenOperationApplication(WebApplication application) : base(application)
    {
    }
}
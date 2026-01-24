using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Scheduler
{
    public class AizenBackgroundJobActivatorScope : JobActivatorScope
    {
        private readonly IServiceScope _serviceScope;

        public AizenBackgroundJobActivatorScope(IServiceScope serviceScope)
        {
            this._serviceScope = serviceScope;
        }

        public override object Resolve(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(this._serviceScope.ServiceProvider, type);
        }
    }
}

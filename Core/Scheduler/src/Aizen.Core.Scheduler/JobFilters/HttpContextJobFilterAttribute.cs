using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Scheduler.JobFilters
{
    public class HttpContextJobFilterAttribute : JobFilterAttribute, IClientFilter, IServerFilter, IElectStateFilter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public HttpContextJobFilterAttribute(IServiceProvider serviceProvider)
        {
            this._httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            this._serviceProvider = serviceProvider;
        }

        public void OnCreating(CreatingContext filterContext)
        {
        }

        public void OnCreated(CreatedContext filterContext)
        {
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            if (this._httpContextAccessor.HttpContext is null)
            {
                this._httpContextAccessor.HttpContext = new DefaultHttpContext
                {
                    RequestServices = this._serviceProvider
                };
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
        }

        public void OnStateElection(ElectStateContext context)
        {
        }
    }
}

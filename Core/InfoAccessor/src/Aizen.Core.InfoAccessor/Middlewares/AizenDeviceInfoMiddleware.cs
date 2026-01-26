using Aizen.Core.Common.Abstraction.Enums;
using Aizen.Core.InfoAccessor.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Aizen.Core.InfoAccessor.Middlewares
{
    internal class AizenDeviceInfoMiddleware
    {
        private readonly RequestDelegate _next;

        public AizenDeviceInfoMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var seyirInfoAccessor = (IAizenInfoAccessor)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoAccessor));
            if (seyirInfoAccessor.DeviceInfoAccessor?.DeviceInfo != null)
            {
                await this._next(httpContext);
                return;
            }

            AizenDeviceInfo deviceInfo;
            if (httpContext?.Request?.Headers == null)
            {
                deviceInfo = AizenDeviceInfo.Unknown;
            }
            else
            {
                _ = Enum.TryParse(httpContext.Request.Headers["X-Aizen-Os-Name"],
                    out DeviceOsType osType);

                deviceInfo = new AizenDeviceInfo
                {
                    OsType = osType,
                    Name = httpContext.Request.Headers["X-Aizen-Device-Name"],
                    Brand = httpContext.Request.Headers["X-Aizen-Device-Brand"],
                    Model = httpContext.Request.Headers["X-Aizen-Device-Model"],
                    OsVersion = httpContext.Request.Headers["X-Aizen-Os-Version"],
                    IpAddress = httpContext.Request.Headers["X-Aizen-Ip-Address"],
                    MacAddress = httpContext.Request.Headers["X-Aizen-Mac-Address"],
                };
            }

            var container = (IAizenInfoContainer)httpContext.RequestServices.GetRequiredService(typeof(IAizenInfoContainer));
            container.Set(deviceInfo);

            await this._next(httpContext);
        }
    }
}

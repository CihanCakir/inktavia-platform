using Aizen.Core.Common.Abstraction.Exception;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLocalizator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Aizen.Core.Common.Extension
{
    public static class BuilderExtensions
    {
        public static IServiceCollection AddAizenException<TException>(this IServiceCollection services, IConfiguration configuration) where TException : AizenException
        {
            var EntryAssembly = Assembly.GetEntryAssembly();

            var path = Path.GetDirectoryName(EntryAssembly.Location);
            var moduleAssemblies = Directory
                .GetFiles(path, "Aizen.*.dll", SearchOption.TopDirectoryOnly)
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToList();

          
            return services;
        }
    }
}

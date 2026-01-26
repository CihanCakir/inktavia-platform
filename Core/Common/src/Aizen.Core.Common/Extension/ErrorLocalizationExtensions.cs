using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Common.Abstraction.Exception;
namespace Aizen.Core.Common.Extension
{
  public static class ErrorLocalizationExtensions
    {
        /// <summary>
        /// ErrorLocalization ayarlarını okur ve AizenException'ı yapılandırır.
        /// </summary>
        public static IServiceCollection AddAizenErrorLocalization(
            this IServiceCollection services,
            IConfiguration configuration,
            Assembly? fallbackAssembly = null)
        {
            AizenException.ConfigureFromConfiguration(configuration, fallbackAssembly);
            return services;
        }
    }
}
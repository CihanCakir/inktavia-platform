using Aizen.Modules.Messaging.Application.Services;
using Aizen.Modules.Messaging.Domain.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Modules.Messaging.Application;

[DocumentationInfo("Messaging Application DI registration",
    "Registers application-layer services: file storage integration and LLM content analyzer.")]
public static class DependencyInjection
{
    public static IServiceCollection AddMessagingApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMessagingFileStorageService, MessagingFileStorageService>();
        services.AddScoped<ILlmContentAnalyzer, AnthropicLlmContentAnalyzer>();
        return services;
    }
}

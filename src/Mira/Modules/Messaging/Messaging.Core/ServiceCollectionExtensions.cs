using Microsoft.Extensions.DependencyInjection;
using Shared.Core;

namespace Messaging.Core;

public static class ServiceCollectionExtensions
{
    public static void AddMessagingService(this IServiceCollection services)
    {
        services.AddTransient<IMessageService, MessageService>();
    }
}
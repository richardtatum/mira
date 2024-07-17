using Microsoft.Extensions.DependencyInjection;

namespace Mira.Features.Messaging;

public static class ServiceCollectionExtensions
{
    public static void AddMessagingService(this IServiceCollection services)
    {
        services.AddTransient<IMessageService, MessageService>();
    }
}
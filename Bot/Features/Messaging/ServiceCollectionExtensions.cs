using Microsoft.Extensions.DependencyInjection;
using Mira.Features.Messaging.Repositories;

namespace Mira.Features.Messaging;

public static class ServiceCollectionExtensions
{
    public static void AddMessagingService(this IServiceCollection services)
    {
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<QueryRepository>();
    }
}
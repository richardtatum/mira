using Microsoft.Extensions.DependencyInjection;
using Polling.Core.Options;
using Polling.Core.Repositories;

namespace Polling.Core;

public static class ServiceCollectionExtensions
{
    public static void AddPollingService(this IServiceCollection services)
    {
        services.AddHostedService<PollingHostService>(); // TODO: Set this to ignore failures?
        services.AddTransient<IHostPollingService, HostPollingService>();
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddTransient<QueryRepository>();
        
        services
            .AddOptions<PollingOptions>()
            .BindConfiguration(nameof(PollingOptions));
    }
}
using Microsoft.Extensions.DependencyInjection;
using Polling.Core.Options;
using Polling.Core.Repositories;

namespace Polling.Core;

public static class ServiceCollectionExtensions
{
    public static void AddPollingService(this IServiceCollection services)
    {
        services.AddHostedService<PollingService>(); // TODO: Set this to ignore failures
        services.AddTransient<QueryRepository>();
        
        services
            .AddOptions<PollingOptions>()
            .BindConfiguration(nameof(PollingOptions));
    }
}
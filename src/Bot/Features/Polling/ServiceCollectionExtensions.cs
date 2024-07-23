using Microsoft.Extensions.DependencyInjection;
using Mira.Features.Polling.Options;
using Mira.Features.Polling.Repositories;

namespace Mira.Features.Polling;

public static class ServiceCollectionExtensions
{
    public static void AddPollingService(this IServiceCollection services)
    {
        services.AddHostedService<PollingService>(); // TODO: Set this to ignore failures

        
        services
            .AddOptions<PollingOptions>()
            .BindConfiguration(nameof(PollingOptions));
    }
}
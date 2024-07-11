using Microsoft.Extensions.DependencyInjection;
using Mira.Features.Polling.Repositories;

namespace Mira.Features.Polling;

public static class ServiceCollectionExtensions
{
    public static void AddPollingService(this IServiceCollection services)
    {
        services.AddSingleton<PollingService>(); // TODO: Set this to ignore failures
        services.AddScoped<StreamStatusService>();
        services.AddScoped<BroadcastBoxClient>();
        services.AddScoped<QueryRepository>();
        services.AddScoped<CommandRepository>();
    }
}
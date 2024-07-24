using Microsoft.Extensions.DependencyInjection;
using Mira.Features.ChangeTracking.Core.Repositories;

namespace Mira.Features.ChangeTracking.Core;

public static class ServiceCollectionExtensions
{
    public static void AddChangeTracking(this IServiceCollection services)
    {
        services.AddTransient<ChangeTrackingService>();
        services.AddTransient<BroadcastBoxClient>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<CommandRepository>();
    }
}
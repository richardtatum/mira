using ChangeTracking.Core.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Interfaces;

namespace ChangeTracking.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddChangeTracking(this IServiceCollection services)
    {
        services.AddTransient<IChangeTrackingService, ChangeTrackingService>();
        services.AddTransient<BroadcastBoxClient>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<CommandRepository>();
    }
}
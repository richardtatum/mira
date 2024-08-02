using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Interfaces;

namespace Snapshot.Core;

public static class ServiceCollectionExtensions
{
    public static void AddSnapshotService(this IServiceCollection services)
    {
        services.AddTransient<CommandRepository>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<ISnapshotService, SnapShotService>();
        services.AddOptions<SnapshotOptions>().BindConfiguration(nameof(SnapshotOptions));
    }
}
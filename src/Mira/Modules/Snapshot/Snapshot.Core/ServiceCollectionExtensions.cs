using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Core.Interfaces;
using Snapshot.Core.Actors;
using Snapshot.Core.Models;

namespace Snapshot.Core;

public static class ServiceCollectionExtensions
{
    public static void AddSnapshotService(this IServiceCollection services)
    {
        services.AddTransient<CommandRepository>();
        services.AddTransient<QueryRepository>();
        services.AddTransient<BroadcastBoxHttpClient>();
        services.AddTransient<ISnapshotService, SnapShotService>();
        services.AddTransient<IWhepConnectionFactory, BroadcastBoxWhepConnectionFactory>();
        services.AddOptions<SnapshotOptions>().BindConfiguration(nameof(SnapshotOptions));
        services.AddSingleton<IActor<ConvertedFrameMessage>, DatabaseWriterActor>();
        services.AddSingleton<IActor<FrameMessage>, ImageConverterActor>(sp =>
        {
            var next = sp.GetRequiredService<IActor<ConvertedFrameMessage>>();
            var logger = sp.GetRequiredService<ILogger<ImageConverterActor>>();
            return new ImageConverterActor(next.Writer, logger);
        });
        services.AddHostedService<ActorBackgroundService<FrameMessage>>();
        services.AddHostedService<ActorBackgroundService<ConvertedFrameMessage>>();
    }
}
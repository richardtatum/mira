using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Snapshot.Core.Actors;

public class ActorBackgroundService<TMessage>(
    IActor<TMessage> actor,
    ILogger<ActorBackgroundService<TMessage>> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting actor ${ActorType}", actor.GetType().Name);
        await actor.StartAsync(cancellationToken: stoppingToken);
    }
}
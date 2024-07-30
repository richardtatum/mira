using ChangeTracking.Core.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Core.Interfaces;
using Stream = ChangeTracking.Core.Models.Stream;

namespace ChangeTracking.Core;

internal class ChangeTrackingService(
    BroadcastBoxClient client,
    ILogger<ChangeTrackingService> logger,
    QueryRepository query,
    CommandRepository command,
    IMessageService messageService) : IChangeTrackingService
{
    public async Task ExecuteAsync(string hostUrl)
    {
        var subscriptions = await query.GetSubscriptionsAsync(hostUrl);
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[CHANGE-TRACKING][{Host}] No key subscriptions found. Skipping.", hostUrl);
            return;
        }

        logger.LogInformation(
            "[CHANGE-TRACKING][{Host}] {Subscriptions} key subscription(s) found. Checking for stream updates.",
            hostUrl, subscriptions.Length);
        var currentStreams = await client.GetStreamsAsync(hostUrl);
        var existingStreams = await query.GetStreamsAsync(subscriptions.Select(x => x.Id));

        var streams = subscriptions
            .Select(async subscription =>
            {
                // Create the stream object which manages the state and any changes
                var existingStream = existingStreams.FirstOrDefault(x => x.SubscriptionId == subscription.Id);
                var currentStream = currentStreams.FirstOrDefault(x => x.StreamKey == subscription.StreamKey);
                var stream = new Stream(hostUrl, subscription, existingStream, currentStream);
                
                // Register events
                stream.OnSendNewMessage += messageService.SendAsync;
                stream.OnSendUpdateMessage += messageService.ModifyAsync;
                stream.OnRecordStateChange += command.UpsertStreamRecord;

                // Fire events
                await stream.FireEventsAsync();
            })
            .ToArray();

        await Task.WhenAll(streams);
    }
}
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
    IMessageService messageService) : IChangeTrackingService // This seems wrong, having polling rely on the message service
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
            .Select(sub =>
            {
                var existingStream = existingStreams.FirstOrDefault(x => x.SubscriptionId == sub.Id);
                var currentStream = currentStreams.FirstOrDefault(x => x.StreamKey == sub.StreamKey);

                return new Stream(hostUrl)
                    .LoadSubscriptionData(sub)
                    .LoadExistingStreamData(existingStream)
                    .LoadCurrentStreamData(currentStream);
            })
            .ToArray();

        var streamUpdates = streams.Where(stream => stream.StreamUpdated).ToArray();

        var newMessageTasks = streamUpdates
            .Where(stream => stream.SendNewMessage)
            .Select(async stream =>
            {
                var (channelId, status, url, viewerCount, duration) = stream.DeconstructIntoNewMessage();
                var messageId = await messageService
                    .SendAsync(channelId, status, url, viewerCount, duration);

                if (messageId is not null)
                {
                    stream.MarkMessageSent(messageId.Value);
                }

                return stream;
            });

        var updateMessageTasks = streamUpdates
            .Where(stream => stream.SendUpdateMessage)
            .Select(async stream =>
            {
                var (messageId, channelId, status, url, viewerCount, duration) = stream.DeconstructIntoUpdateMessage();
                await messageService
                    .ModifyAsync(messageId, channelId, status, url, viewerCount, duration);
                return stream;
            });

        streamUpdates = await Task.WhenAll(newMessageTasks.Union(updateMessageTasks));

        var upsertRecordsTasks = streamUpdates
            .Select(stream => command.UpsertStreamRecord(stream.ToStreamRecord()));

        await Task.WhenAll(upsertRecordsTasks);
    }
}
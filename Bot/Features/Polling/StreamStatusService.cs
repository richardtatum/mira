using Microsoft.Extensions.Logging;
using Mira.Features.Messaging;
using Mira.Features.Polling.Models;
using Mira.Features.Polling.Repositories;
using Mira.Features.Shared;
using Host = Mira.Features.Polling.Models.Host;

namespace Mira.Features.Polling;

public class StreamStatusService(
    BroadcastBoxClient client,
    ILogger<StreamStatusService> logger,
    QueryRepository query,
    CommandRepository command,
    IMessageService messageService) // This seems wrong, having polling rely on the message service
{

    internal async Task UpdateStreamsAsync(Host host, Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[NOTIFICATION-SERVICE][{Host}] No subscriptions provided. Skipping.", host.Url);
            return;
        }

        var currentLiveStreams = await GetSubscribedLiveStreamsAsync(host, subscriptions);
        var existingLiveStreams = await query.GetLiveStreamsAsync(subscriptions.Select(x => x.Id));
        
        var streamOverviews = GenerateStreamOverviews(currentLiveStreams, existingLiveStreams);

        // This doesn't work if the message fails to send due to incorrect channel
        var messageTasks = streamOverviews.Select(async stream =>
        {
            var messageId = stream.MessageId is null
                ? await messageService.SendAsync(stream.ChannelId, stream.Status, stream.Url,
                    stream.ViewerCount,
                    stream.StartTime)
                : await messageService.UpdateAsync(stream.MessageId.Value, stream.ChannelId, stream.Status,
                    stream.Url,
                    stream.ViewerCount,
                    stream.StartTime, stream.EndTime);
            return (messageId, stream);
        });

        var results = await Task.WhenAll(messageTasks);
        
        // This just skips anything that fails to send as a message, is this a problem? Potentially leaves records of open streams?
        var upsertTasks = results
            .Where(result => result.messageId is not null)
            .Select(result =>
            {
                result.stream.MessageId = result.messageId;
                return command.UpsertStreamRecord(result.stream.ToStreamRecord());
            })
            .ToArray();

        await Task.WhenAll(upsertTasks);
    }

    internal IEnumerable<StreamOverview> GenerateStreamOverviews(
        IEnumerable<LiveStream> currentLiveStreams, IEnumerable<StreamOverview> existingLiveStreams)
    {
        foreach (var stream in currentLiveStreams)
        {
            var existingStream = existingLiveStreams.FirstOrDefault(x => x.SubscriptionId == stream.SubscriptionId);
            yield return new StreamOverview
            {
                Id = existingStream?.Id,
                SubscriptionId = stream.SubscriptionId,
                StreamKey = stream.StreamKey,
                HostUrl = stream.HostUrl,
                Status = StreamStatus.Live,
                StartTime = existingStream?.StartTime ?? DateTime.UtcNow,
                ViewerCount = stream.ViewerCount,
                MessageId = existingStream?.MessageId,
                ChannelId = stream.ChannelId
            };
        }

        // Get all the existing streams missing from the current live list
        var offlineStreams = existingLiveStreams
            .Where(record => !currentLiveStreams
                .Select(stream => stream.SubscriptionId)
                .Contains(record.SubscriptionId)
            )
            .ToArray();

        foreach (var stream in offlineStreams)
        {
            stream.Status = StreamStatus.Offline;
            stream.EndTime = DateTime.UtcNow;
            yield return stream;
        }
    }
    
    internal async Task<LiveStream[]> GetSubscribedLiveStreamsAsync(Host host, Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[NOTIFICATION-SERVICE][{Host}] No subscriptions provided. Skipping.", host.Url);
            return [];
        }
        
        var streams = await client.GetStreamsAsync(host.Url);
        var liveStreams = streams.Where(stream => stream.IsLive).ToArray();
        
        return subscriptions
            .Where(subscription => liveStreams.Select(stream => stream.StreamKey).Contains(subscription.StreamKey))
            .Select(subscription =>
            {
                var stream = liveStreams.First(stream => stream.StreamKey == subscription.StreamKey);
                return new LiveStream
                {
                    SubscriptionId = subscription.Id,
                    StreamKey = subscription.StreamKey,
                    HostUrl = host.Url,
                    ViewerCount = stream.CurrentViewers,
                    ChannelId = subscription.ChannelId
                };
            })
            .ToArray();
    }
}
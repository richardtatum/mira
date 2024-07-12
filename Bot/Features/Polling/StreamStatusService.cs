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

        // THIS IS SLOW!
        foreach (var overview in streamOverviews)
        {
            if (overview.MessageId is not null)
            {
                await messageService.UpdateAsync(overview.MessageId.Value, overview.ChannelId, overview.Status, overview.Url,
                    overview.ViewerCount,
                    overview.StartTime, overview.EndTime);

                await command.UpsertStreamRecord(overview.ToStreamRecord());
                continue;
            }
            
            var messageId = await messageService.SendAsync(overview.ChannelId, overview.Status, overview.Url,
                overview.ViewerCount,
                overview.StartTime);
            
            if (messageId is null)
            {
                logger.LogError("[NOTIFICATION-SERVICE][{Host}] Failed to send notification message Channel: {Channel}", overview.ChannelId);
                continue;
            }

            overview.MessageId = messageId;
            await command.UpsertStreamRecord(overview.ToStreamRecord());
        }
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
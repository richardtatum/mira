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

        var subscriptionIds = subscriptions.Select(x => x.Id);
        var existingLiveStreams = await query.GetLiveStreamsAsync(subscriptionIds);
       
        var currentLiveStreams = await GetSubscribedLiveStreamsAsync(host, subscriptions);
        var records = GenerateStreamRecords(currentLiveStreams, existingLiveStreams);

        var upsertTasks = records.Select(command.UpsertStreamRecord);
        await Task.WhenAll(upsertTasks);

        // We only notify about records that are to be created (Id is null) or have just been marked as offline
        // TODO: Need to switch this to send new messages, update existing ones (with new viewer count)
        var notifications = records.Where(record => record.Id is null || record.Status == StreamStatus.Offline);
        var messageTasks = notifications.Select(notify => messageService.SendAsync(notify.SubscriptionId));
        await Task.WhenAll(messageTasks);
    }

    internal IEnumerable<StreamRecord> GenerateStreamRecords(
        IEnumerable<LiveStream> currentLiveStreams, IEnumerable<StreamRecord> existingLiveStreams)
    {
        // TODO: YOU NEED TO SPECIFY START TIME FOR NEW STREAMS OTHERWISE IT MAY USE THE LAST STREAMS STARTTIME
        foreach (var stream in currentLiveStreams)
        {
            var existingStream = existingLiveStreams.FirstOrDefault(x => x.SubscriptionId == stream.SubscriptionId);
            yield return new StreamRecord
            {
                Id = existingStream?.Id,
                SubscriptionId = stream.SubscriptionId,
                Status = StreamStatus.Live,
                StartTime = existingStream?.StartTime ?? DateTime.UtcNow,
                ViewerCount = stream.ViewerCount
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
                    Host = host.Url,
                    ViewerCount = stream.CurrentViewers,
                };
            })
            .ToArray();
    }
}
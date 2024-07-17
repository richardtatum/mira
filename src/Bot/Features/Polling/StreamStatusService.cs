using Microsoft.Extensions.Logging;
using Mira.Features.Messaging;
using Mira.Features.Polling.Models;
using Mira.Features.Polling.Repositories;
using Mira.Features.Shared;
using Host = Mira.Features.Polling.Models.Host;

namespace Mira.Features.Polling;

// TODO: Update to use the stream start time from the endpoint, rather than UTCNOW
// TODO: Separate/uncouple the messaging service from the polling/stream updates?
public class StreamStatusService(
    BroadcastBoxClient client,
    ILogger<StreamStatusService> logger,
    QueryRepository query,
    CommandRepository command,
    IMessageService messageService) // This seems wrong, having polling rely on the message service
{
    // This is the actual stream data
    public record Stream(string Key, string HostUrl, int Viewers, DateTime StartTime);
    
    // Below is a Discord consideration
    public record Notification(ulong ChannelId, string Key, string HostUrl, int Viewers, TimeSpan Duration);
    public record UpdateNotification(ulong MessageId, ulong ChannelId, StreamStatus status, string Key, string HostUrl, int Viewers, TimeSpan Duration);
    
        
    // Below two are DB considerations
    public record LiveStream(int StreamId, string Key, string HostUrl, int Viewers, DateTime StartTime);
    public record OfflineStream(int StreamId, string Key, string HostUrl, DateTime EndTime);
    
    public async Task<Stream[]> GetLiveStreamsAsync(string hostUrl, IEnumerable<string> subscribedStreamKeys)
    {
        var streamKeys = subscribedStreamKeys as string[] ?? subscribedStreamKeys.ToArray();
        if (streamKeys.Length == 0)
        {
            logger.LogInformation("[STATUS-SERVICE][{Host}] No subscriptions provided. Skipping.", hostUrl);
            return [];
        }

        var streams = await client.GetStreamsAsync(hostUrl);
        return streams
            .Where(stream => stream.IsLive && streamKeys.Contains(stream.StreamKey))
            .Select(stream => new Stream(stream.StreamKey, hostUrl, stream.Viewers, stream.StartTime))
            .ToArray();
    }

    public async Task<Notification[]> GetNotificationsAsync(string hostUrl, IEnumerable<Stream> streams)
    {
        var liveStreams = streams as Stream[] ?? streams.ToArray();
        
        // Need to know from existing streams:
        // - StreamKey to match on
        // - ChannelId
        // - GuildId
        // - StartTime to give final duration
        // - If it was previously live
        // - If it's no longer in this list
        var existingStreams = await query.GetStreamsAsync(hostUrl);


        var newStreams = new List<Notification>();
        var updates = new List<UpdateNotification>();

        foreach (var stream in liveStreams)
        {
            var duration = DateTime.UtcNow.Subtract(stream.StartTime);
            var existingStream = existingStreams.FirstOrDefault(s => s.StreamKey == stream.Key);
            // Either no stream record, or previously marked as offline
            if (existingStream?.Status != StreamStatus.Live)
            {
                var channelId = await query.GetChannelIdAsync(stream.HostUrl, stream.Key);
                if (channelId is null)
                {
                    // Something has gone very wrong
                    continue;
                }
                
                newStreams.Add(new Notification(channelId.Value, stream.Key, stream.HostUrl, stream.Viewers, duration));
                continue;
            }
            
            // Its previously marked as live
            updates.Add(new UpdateNotification(existingStream.MessageId, existingStream.ChannelId, StreamStatus.Live, stream.Key,
                stream.HostUrl, stream.Viewers, duration));
        }
        
        // Its missing, so it's now offline
        var liveStreamKeys = liveStreams.Select(x => x.Key);
        var offlineStreams = existingStreams
            .Where(x => x.Status == StreamStatus.Live && !liveStreamKeys.Contains(x.StreamKey))
            .Select(x => new UpdateNotification(x.MessageId, x.ChannelId, StreamStatus.Offline, x.StreamKey, hostUrl, 0, DateTime.UtcNow.Subtract(x.StartTime)))
            .ToArray();
        
        updates.AddRange(offlineStreams);
    }

    public async Task<(StreamSummary[] newStreams, StreamSummary[] updates)> GetStreamChangesAsync(string hostUrl, IEnumerable<StreamSummary> liveStreams)
    {
        var streams = liveStreams as StreamSummary[] ?? liveStreams.ToArray();
        var existingStreams = await query.GetStreamsAsync(hostUrl);

        var newStreams = new List<StreamSummary>();
        var updates = new List<StreamSummary>();

        foreach (var stream in streams)
        {
            var existingStream = existingStreams.FirstOrDefault(x => x.StreamKey == stream.Key);
            // Either no stream record, or previously marked as offline
            if (existingStream?.Status != StreamStatus.Live)
            {
                newStreams.Add(stream);
                continue;
            }
            
             // Its previously marked as live
             var streamUpdate =
                 new StreamSummary(existingStream.Id, stream.Key, hostUrl, existingStream.StartTime, null);
             updates.Add(stream);
        }

        // Its missing, so it's now offline
        var liveStreamKeys = streams.Select(x => x.Key);
        var offlineStreams = existingStreams
            .Where(x => x.Status == StreamStatus.Live && !liveStreamKeys.Contains(x.StreamKey))
            .Select(x => new StreamSummary(x.Id, x.StreamKey, hostUrl, x.StartTime, DateTime.UtcNow))
            .ToArray();
        
        updates.AddRange(offlineStreams);

        return (newStreams.ToArray(), updates.ToArray());
    }

    public Task<ulong?> NewMessageAsync(ulong channelId, StreamSummary stream)
    {
        return messageService.SendAsync(channelId, stream.Status, stream.Url, stream.Viewers, stream.Duration);
    }

    public Task<ulong?> ModifyMessageAsync(ulong messageId, ulong channelId, StreamSummary stream)
    {
        return messageService.ModifyAsync(messageId, channelId, stream.Status, stream.Url, stream.Viewers, stream.Duration);
    }
    
    
    
    // I am still not happy with this approach. Probably makes more sense to do event based however I am worried about even more
    // running background services
    internal async Task UpdateStreamsAsync(Host host, Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[STATUS-SERVICE][{Host}] No subscriptions provided. Skipping.", host.Url);
            return;
        }

        var currentLiveStreams = await GetSubscribedLiveStreamsAsync(host, subscriptions);
        var existingLiveStreams = await query.GetLiveStreamsAsync(subscriptions.Select(x => x.Id));
        var streams = GenerateStreamOverviews(currentLiveStreams, existingLiveStreams);
        if (!streams.Any())
        {
            logger.LogInformation("[STATUS-SERVICE][{Host}] No stream updates found. Skipping.", host.Url);
            return;
        }

        logger.LogInformation("[STATUS-SERVICE][{Host}] {Count} update(s) found. Preparing messages.", host.Url, streams.Count());
        var messageTasks = streams.Select(async stream =>
        {
            var messageId = await SendMessageAsync(stream);
            return (messageId, stream);
        });

        var results = await Task.WhenAll(messageTasks);

        // TODO: Update this to mark any that have failed to send as invalid and remove them with a notification?
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
            logger.LogInformation("[STATUS-SERVICE][{Host}] No subscriptions provided. Skipping.", host.Url);
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
                    ViewerCount = stream.Viewers,
                    ChannelId = subscription.ChannelId
                };
            })
            .ToArray();
    }

    private Task<ulong?> SendMessageAsync(StreamOverview stream) => stream.MessageId is not null
        ? messageService.ModifyAsync(stream.MessageId.Value, stream.ChannelId, stream.Status,
            stream.Url,
            stream.ViewerCount,
            stream.Duration)
        : messageService.SendAsync(stream.ChannelId, stream.Status, stream.Url,
            stream.ViewerCount,
            stream.Duration);
}
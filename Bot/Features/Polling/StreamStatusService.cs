using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Mira.Features.Messaging;
using Mira.Features.Polling.Models;
using Mira.Features.Polling.Repositories;
using Host = Mira.Features.Polling.Models.Host;

namespace Mira.Features.Polling;

public class StreamStatusService(
    BroadcastBoxClient client,
    ILogger<StreamStatusService> logger,
    QueryRepository query,
    CommandRepository command,
    DiscordSocketClient discord)
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
       
        var currentStreams = await GetStreamSummariesAsync(host, subscriptions);
        var records = GenerateStreamRecords(currentStreams, existingLiveStreams);

        var upsertTasks = records.Select(command.UpsertStreamRecord);
        var messageTasks = records.Select(stream => SendStatusMessage(stream.SubscriptionId, stream.Status));

        await Task.WhenAll(upsertTasks.Union(messageTasks));
    }

    internal StreamRecord[] GenerateStreamRecords(
        IEnumerable<StreamSummary> currentStreams, IEnumerable<StreamRecord> existingLiveStreams)
    {
        var liveStreams = currentStreams.Where(stream => stream.IsLive).ToArray();
        var offlineStreams = currentStreams.Where(stream => !stream.IsLive).ToArray();
        
        var ongoingStreams = existingLiveStreams
            .Where(existingStream => liveStreams.Select(stream => stream.SubscriptionId).Contains(existingStream.SubscriptionId))
            .ToArray();
        
        var newLiveStreams = liveStreams
            .Where(stream => !existingLiveStreams.Select(x => x.SubscriptionId).Contains(stream.SubscriptionId))
            .Select(stream => new StreamRecord
            {
                SubscriptionId = stream.SubscriptionId,
                Status = StreamStatus.Live,
                StartTime = DateTime.UtcNow
            })
            .ToArray();
        
        var newOfflineStreams = existingLiveStreams
            .Where(stream => offlineStreams.Select(stream => stream.SubscriptionId).Contains(stream.SubscriptionId))
            .Select(stream => new StreamRecord
            {
                Id = stream.Id,
                SubscriptionId = stream.SubscriptionId,
                Status = StreamStatus.Offline,
                StartTime = stream.StartTime,
                EndTime = DateTime.UtcNow,
            })
            .ToArray();

        return ongoingStreams
            .Union(newLiveStreams)
            .Union(newOfflineStreams)
            .ToArray();
    }
    
    internal async Task<StreamSummary[]> GetStreamSummariesAsync(Host host, Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[NOTIFICATION-SERVICE][{Host}] No subscriptions provided. Skipping.", host.Url);
            return [];
        }
        
        var streams = await client.GetStreamsAsync(host.Url);
        return subscriptions
            .Select(subscription =>
            {
                var stream = streams.FirstOrDefault(stream => stream.StreamKey == subscription.StreamKey);
                return new StreamSummary
                {
                    SubscriptionId = subscription.Id,
                    StreamKey = subscription.StreamKey,
                    Host = host.Url,
                    Viewers = stream?.CurrentViewers ?? 0,
                    IsLive = stream?.IsLive ?? false
                };
            })
            .ToArray();
    }

    // Pass the message to this, some abstraction of a component perhaps?
    private async Task SendStatusMessage(int subscriptionId, StreamStatus status)
    {
        var stream = await query.GetStreamSummaryAsync(subscriptionId);

        if (discord.GetChannel(stream.Channel) is not IMessageChannel channel)
        {
            logger.LogCritical("[NOTIFICATION-SERVICE] Failed to retrieve channel for subscriptionId {SubscriptionId}. Channel: {Channel}", subscriptionId, stream.Channel);
            return;
        }

        switch (status)
        {
            case StreamStatus.Live:
                var button = new ComponentBuilder().WithButton("Watch now!", style: ButtonStyle.Link, url: stream.Url).Build();
                await channel.SendMessageAsync($"Stream is live! Key: {stream.StreamKey}", components: button);
                break;
            case StreamStatus.Offline:
                await channel.SendMessageAsync($"Stream is over! Key: {stream.StreamKey}, Length: {stream.Duration?.TotalMinutes} minute(s)");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}
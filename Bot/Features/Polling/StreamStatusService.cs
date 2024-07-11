using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
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
       
        var (live, offline) = await GetCurrentStatusAsync(host, subscriptions);
        var (newLiveStreams, newOfflineStreams) = GetStatusChanges(live, offline, existingLiveStreams);

        // Update records in DB
        var updateTasks = newOfflineStreams
            .Union(newLiveStreams)
            .Select(command.UpsertStreamRecord);

        await Task.WhenAll(updateTasks);

        // Send Messages
        var messageTasks = newLiveStreams.Union(newOfflineStreams)
            .Select(stream => SendStatusMessage(stream.SubscriptionId, stream.Status));

        await Task.WhenAll(messageTasks);
    }

    internal (StreamRecord[] live, StreamRecord[] offline) GetStatusChanges(
        IEnumerable<Subscription> liveStreams, IEnumerable<Subscription> offlineStreams, IEnumerable<StreamRecord> existingLiveStreams)
    {
        var liveSubscriptions = liveStreams as Subscription[] ?? liveStreams.ToArray();
        var offlineSubscriptions = offlineStreams as Subscription[] ?? offlineStreams.ToArray();
        
        var newLiveStreams = liveSubscriptions
            .Where(subscription => !existingLiveStreams.Select(x => x.SubscriptionId).Contains(subscription.Id))
            .Select(subscription => new StreamRecord
            {
                SubscriptionId = subscription.Id,
                Status = StreamStatus.Live,
                StartTime = DateTime.UtcNow
            })
            .ToArray();
        
        var newOfflineStreams = existingLiveStreams
            .Where(stream => offlineSubscriptions.Select(x => x.Id).Contains(stream.SubscriptionId))
            .Select(stream => new StreamRecord
            {
                Id = stream.Id,
                SubscriptionId = stream.SubscriptionId,
                Status = StreamStatus.Offline,
                StartTime = stream.StartTime,
                EndTime = DateTime.UtcNow,
            })
            .ToArray();

        return (newLiveStreams, newOfflineStreams);
    }
    
    internal async Task<(Subscription[] live, Subscription[] offline)> GetCurrentStatusAsync(Host host,
        Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            logger.LogInformation("[NOTIFICATION-SERVICE][{Host}] No subscriptions provided. Skipping.", host.Url);
            return ([], []);
        }
        
        var streams = await client.GetStreamsAsync(host.Url);
        var liveStreamKeys = streams.Where(stream => stream.IsLive).Select(stream => stream.StreamKey);

        var subscribedLiveStreams = subscriptions
            .Where(subscription => liveStreamKeys.Contains(subscription.StreamKey))
            .ToArray();

        var subscribedOfflineStreams = subscriptions
            .Except(subscribedLiveStreams)
            .ToArray();

        return (subscribedLiveStreams, subscribedOfflineStreams);
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
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Mira.Features.Shared.Models;
using Mira.Features.StreamChecker.Repositories;

namespace Mira.Features.StreamChecker;

public class StreamNotificationService
{
    private readonly BroadcastBoxClient _client;
    private readonly ILogger<StreamNotificationService> _logger;
    private readonly QueryRepository _query;
    private readonly CommandRepository _command;
    private readonly DiscordSocketClient _discord;

    public StreamNotificationService(BroadcastBoxClient client, ILogger<StreamNotificationService> logger, QueryRepository query, CommandRepository command, DiscordSocketClient discord)
    {
        _client = client;
        _logger = logger;
        _query = query;
        _command = command;
        _discord = discord;
    }

    // What if the same streamKey and host across multiple servers?
    // TODO: Better name, less generic. SOLID principles mean break this down! Split INTERFACES!
    // TODO: Move this into its own service that calls the notification service to create/send the message
    internal async Task CheckStreamsAsync(string hostUrl, Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            _logger.LogInformation("[NOTIFICATION-SERVICE] No subscriptions provided for host {Host}. Skipping.", hostUrl);
            return;
        }
        

        var streams = await _client.GetStreamsAsync(hostUrl);
        var liveStreamKeys = streams.Where(stream => stream.IsLive).Select(stream => stream.StreamKey);
        
        var subscribedLiveStreams = subscriptions
            .Where(subscription => liveStreamKeys.Contains(subscription.StreamKey))
            .ToArray();
        var subscribedOfflineStreams = subscriptions
            .Except(subscribedLiveStreams)
            .ToArray();
        
        // TODO: Better handle these null Ids, maybe a DTO?
        // These are streams recorded as being live right now in the system
        var subscriptionIds = subscriptions.Select(x => x.Id ?? 0).Where(x => x != 0).ToArray();
        var liveStreams = await _query.GetLiveStreamsAsync(subscriptionIds);

        var newOfflineStreams = liveStreams
            .Where(stream => subscribedOfflineStreams.Select(x => x.Id).Contains(stream.SubscriptionId))
            .Select(stream => new StreamRecord
            {
                Id = stream.Id,
                SubscriptionId = stream.SubscriptionId,
                Status = StreamStatus.Offline,
                StartTime = stream.StartTime,
                EndTime = DateTime.UtcNow,
            })
            .ToArray();
        
        var newLiveStreams = subscribedLiveStreams
            .Where(subscription => !liveStreams.Select(x => x.SubscriptionId).Contains(subscription.Id ?? 0))
            .Select(subscription => new StreamRecord
            {
                SubscriptionId = subscription.Id ?? 0,
                Status = StreamStatus.Live,
                StartTime = DateTime.UtcNow
            })
            .ToArray();

        foreach (var record in newOfflineStreams.Union(newLiveStreams))
        {
            await _command.UpsertStreamRecord(record);
            await SendStatusMessage(record.SubscriptionId, record.Status);
        }
    }

    // Pass the message to this, some abstraction of a component perhaps?
    private async Task SendStatusMessage(int subscriptionId, StreamStatus status)
    {
        var subscription = await _query.GetSubscriptionSummaryAsync(subscriptionId);

        if (_discord.GetChannel(subscription.Channel) is not IMessageChannel channel)
        {
            _logger.LogCritical("[NOTIFICATION-SERVICE] Failed to retrieve channel for subscriptionId {SubscriptionId}. Channel: {Channel}", subscriptionId, subscription.Channel);
            return;
        }

        switch (status)
        {
            case StreamStatus.Live:
                var button = new ComponentBuilder().WithButton("Watch now!", style: ButtonStyle.Link, url: subscription.Url).Build();
                await channel.SendMessageAsync($"Stream is live! Key: {subscription.StreamKey}", components: button);
                break;
            case StreamStatus.Offline:
                await channel.SendMessageAsync($"Stream is over! Key: {subscription.StreamKey}, Length: {subscription.Duration?.TotalMinutes} minute(s)");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}
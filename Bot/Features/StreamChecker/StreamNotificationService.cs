using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Mira.Features.StreamChecker.Models;
using Mira.Features.StreamChecker.Repositories;
using Host = Mira.Features.StreamChecker.Models.Host;

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
    internal async Task CheckStreamsAsync(Host host, Subscription[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            _logger.LogInformation("[NOTIFICATION-SERVICE] No subscriptions provided for hostId {Host}. Skipping.", host.Url);
            return;
        }
        
        var streams = await _client.GetStreamsAsync(host.Url);
        var liveStreamKeys = streams.Where(stream => stream.IsLive).Select(stream => stream.StreamKey);
        
        var subscribedLiveStreams = subscriptions
            .Where(subscription => liveStreamKeys.Contains(subscription.StreamKey))
            .ToArray();

        var subscribedOfflineStreams = subscriptions
            .Except(subscribedLiveStreams)
            .ToArray();
        
        // These are streams marked as live in the database
        var recordedLiveStreams = await _query.GetLiveStreamsAsync(subscriptions.Select(x => x.Id));

        var newOfflineStreams = recordedLiveStreams
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
            .Where(subscription => !recordedLiveStreams.Select(x => x.SubscriptionId).Contains(subscription.Id))
            .Select(subscription => new StreamRecord
            {
                SubscriptionId = subscription.Id,
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
        var stream = await _query.GetStreamSummaryAsync(subscriptionId);

        if (_discord.GetChannel(stream.Channel) is not IMessageChannel channel)
        {
            _logger.LogCritical("[NOTIFICATION-SERVICE] Failed to retrieve channel for subscriptionId {SubscriptionId}. Channel: {Channel}", subscriptionId, stream.Channel);
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
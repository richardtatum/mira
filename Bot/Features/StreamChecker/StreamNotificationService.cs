using System.Diagnostics;
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
    internal async Task CheckStreamsAsync(Host host, Notification[] notifications)
    {
        if (notifications.Length == 0)
        {
            _logger.LogInformation("[NOTIFICATION-SERVICE] No notifications provided for host {Host}. Skipping.", host.Url);
            return;
        }
        
        // TODO: Better handle these null Ids, maybe a DTO?
        var notificationIds = notifications.Select(x => x.Id ?? 0).Where(x => x != 0).ToArray();
        var streamKeys = await _client.GetStreamKeysAsync(host.Url);
        var liveStreamKeys = streamKeys.Where(stream => stream.IsLive).Select(stream => stream.StreamKey);
        
        var subscribedLiveStreams = notifications
            .Where(notification => liveStreamKeys.Contains(notification.StreamKey))
            .ToArray();
        var subscribedOfflineStreams = notifications
            .Except(subscribedLiveStreams)
            .ToArray();
        
        // These are streams recorded as being live right now in the system
        var liveStreams = await _query.GetLiveStreamsAsync(notificationIds);

        var newOfflineStreams = liveStreams
            .Where(stream => subscribedOfflineStreams.Select(x => x.Id).Contains(stream.NotificationId))
            .Select(stream => new StreamRecord
            {
                Id = stream.Id,
                NotificationId = stream.NotificationId,
                Status = StreamStatus.Offline,
                StartTime = stream.StartTime,
                EndTime = DateTime.UtcNow,
            })
            .ToArray();
        
        var newLiveStreams = subscribedLiveStreams
            .Where(stream => !liveStreams.Select(x => x.NotificationId).Contains(stream.Id ?? 0))
            .Select(notification => new StreamRecord
            {
                NotificationId = notification.Id ?? 0,
                Status = StreamStatus.Live,
                StartTime = DateTime.UtcNow
            })
            .ToArray();

        foreach (var record in newOfflineStreams.Union(newLiveStreams))
        {
            await _command.UpsertStreamRecord(record);
            await SendStatusMessage(record.NotificationId, record.Status);
        }
    }

    private async Task SendStatusMessage(int notificationId, StreamStatus status)
    {
        var notification = await _query.GetNotificationSummaryAsync(notificationId);

        if (_discord.GetChannel(notification.Channel) is not IMessageChannel channel)
        {
            _logger.LogCritical("[NOTIFICATION-SERVICE] Failed to retrieve channel for notificationId {NotificationId}. Channel: {Channel}", notificationId, notification.Channel);
            return;
        }

        switch (status)
        {
            case StreamStatus.Live:
                var button = new ComponentBuilder().WithButton("Watch now!", style: ButtonStyle.Link, url: notification.Url).Build();
                await channel.SendMessageAsync($"Stream is live! Key: {notification.StreamKey}", components: button);
                break;
            case StreamStatus.Offline:
                await channel.SendMessageAsync($"Stream is over! Key: {notification.StreamKey}, Length: {notification.Duration?.TotalMinutes} minute(s)");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}
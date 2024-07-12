using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Mira.Features.Messaging.Repositories;
using Mira.Features.Shared;

namespace Mira.Features.Messaging;

public class MessageService(DiscordSocketClient discord, ILogger<MessageService> logger, QueryRepository queryRepository) : IMessageService
{
    public async Task<IUserMessage?> SendAsync(int subscriptionId)
    {
        // I'm not entirely certain this is right, but I needed to get this refactor closed out for the night
        var stream = await queryRepository.GetStreamSummaryAsync(subscriptionId);
        return await SendAsync(stream.ChannelId, stream.Status, stream.Url, stream.ViewerCount, stream.StartTime,
            stream.EndTime);
    }

    public Task<IUserMessage?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, DateTime startTime,
        DateTime? endTime = null)
    {
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return Task.FromResult((IUserMessage?)null);
        }

        var duration = (endTime ?? DateTime.UtcNow).Subtract(startTime).ToString(@"hh\:mm");
        
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration),
            StreamStatus.Offline => MessageEmbed.Offline(url, viewers, duration),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        return channel.SendMessageAsync(embed: embed);
    }

    public Task<IUserMessage?> UpdateAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, DateTime startTime,
        DateTime? endTime = null)
    {
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return Task.FromResult((IUserMessage?)null);
        }
        
        var duration = (endTime ?? DateTime.UtcNow).Subtract(startTime).ToString(@"hh\:mm");
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration),
            StreamStatus.Offline => MessageEmbed.Offline(url, viewers, duration),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        // Updating the embed means that the stream started with localized time doesn't work
        return channel.ModifyMessageAsync(messageId, properties => properties.Embed = embed);
    }

    private IMessageChannel? GetChannel(ulong channelId) => discord.GetChannel(channelId) as IMessageChannel;
}
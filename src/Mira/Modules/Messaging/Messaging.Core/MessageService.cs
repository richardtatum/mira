using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Shared.Core;
using Shared.Core.Interfaces;

namespace Messaging.Core;

public class MessageService(DiscordSocketClient discord, ILogger<MessageService> logger) : IMessageService
{
    public async Task<ulong?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string playing = null)
    {
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return null;
        }

        // We are never sending a new offline message, should this be simplified?
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration, playing),
            StreamStatus.Offline => MessageEmbed.Offline(url, duration, playing),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
        
        var message = await channel.SendMessageAsync(embed: embed);
        return message.Id;
    }

    public async Task<ulong?> ModifyAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing)
    {
        // TODO: Check validity off messageId too
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return null;
        }
        
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration, playing),
            StreamStatus.Offline => MessageEmbed.Offline(url, duration, playing),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        var message = await channel.ModifyMessageAsync(messageId, properties => properties.Embed = embed);
        return message.Id;
    }

    private IMessageChannel? GetChannel(ulong channelId) => discord.GetChannel(channelId) as IMessageChannel;
}
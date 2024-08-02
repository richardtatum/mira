using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Shared.Core;
using Shared.Core.Interfaces;

namespace Messaging.Core;

public class MessageService(DiscordSocketClient discord, ILogger<MessageService> logger) : IMessageService
{
    public async Task<ulong?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, string? filePath = null)
    {
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return null;
        }

        var imageName = Path.GetFileName(filePath);

        // We are never sending a new offline message, should this be simplified?
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration, playing, imageName),
            StreamStatus.Offline => MessageEmbed.Offline(url, duration, playing),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        var message = filePath is null
            ? await channel.SendMessageAsync(embed: embed) 
            : await channel.SendFileAsync(filePath, embed: embed);

        return message.Id;
    }

    public async Task ModifyAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, string? filePath = null)
    {
        // TODO: Check validity off messageId too
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return;
        }
        
        var imageName = Path.GetFileName(filePath);
        
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration, playing, imageName),
            StreamStatus.Offline => MessageEmbed.Offline(url, duration, playing, null),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
        
        var attachments = filePath is null
            ? Array.Empty<FileAttachment>()
            : [new FileAttachment(filePath)];
        
        await channel.ModifyMessageAsync(messageId, properties =>
        {
            properties.Embed = embed;
            properties.Attachments = attachments;
        });
    }

    private IMessageChannel? GetChannel(ulong channelId) => discord.GetChannel(channelId) as IMessageChannel;
}
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Shared.Core;
using Shared.Core.Interfaces;

namespace Messaging.Core;

public class MessageService(DiscordSocketClient discord, ILogger<MessageService> logger) : IMessageService
{
    public async Task<ulong?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, byte[]? image = null)
    {
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return null;
        }

        var imageName = "image.webp";

        // We are never sending a new offline message, should this be simplified?
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration, playing, imageName),
            StreamStatus.Offline => MessageEmbed.Offline(url, duration, playing, imageName),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        if (image is null)
        {
            var message = await channel.SendMessageAsync(embed: embed);
            return message.Id;
        }

        using var stream = new MemoryStream(image);
        var fileMessage = await channel.SendFileAsync(stream, imageName, embed: embed);

        return fileMessage.Id;
    }

    public async Task ModifyAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, byte[]? image = null)
    {
        // TODO: Check validity off messageId too
        var channel = GetChannel(channelId);
        if (channel is null)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Failed to retrieve channel for stream {Url}. Channel: {Channel}", url, channelId);
            return;
        }
        
        var imageName = "image.webp";
        
        var embed = status switch
        {
            StreamStatus.Live => MessageEmbed.Live(url, viewers, duration, playing, imageName),
            StreamStatus.Offline => MessageEmbed.Offline(url, duration, playing, imageName),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        try
        {
            if (image is null)
            {
                await channel.ModifyMessageAsync(messageId, properties =>
                {
                    properties.Embed = embed;
                    properties.Attachments = Array.Empty<FileAttachment>();
                });
                return;
            }
            
            using var stream = new MemoryStream(image);
            var attachments = new FileAttachment[] { new (stream, imageName) };
            await channel.ModifyMessageAsync(messageId, properties =>
            {
                properties.Embed = embed;
                properties.Attachments = attachments;
            });
        }
        catch (Exception ex)
        {
            logger.LogCritical("[MESSAGE-SERVICE] Message Failed to Update: {Ex}", ex);
            throw;
        }
    }

    private IMessageChannel? GetChannel(ulong channelId) => discord.GetChannel(channelId) as IMessageChannel;
}
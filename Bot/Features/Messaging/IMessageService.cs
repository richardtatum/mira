using Discord;
using Mira.Features.Shared;

namespace Mira.Features.Messaging;

public interface IMessageService
{
    Task<IUserMessage?> SendAsync(int subscriptionId); 
    Task<IUserMessage?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, DateTime startTime, DateTime? endTime = null);
    Task<IUserMessage> UpdateAsync(ulong messageId, StreamStatus status, string url, int viewers, DateTime startTime, DateTime? endTime = null);
    Task<IUserMessage> SendAsync(ulong channelId, string? text = null, Embed? embed = null);
    Task UpdateAsync(ulong messageId, string? text = null, Embed? embed = null);
}
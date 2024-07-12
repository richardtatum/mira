using Discord;
using Mira.Features.Shared;

namespace Mira.Features.Messaging;

public interface IMessageService
{
    Task<ulong?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, DateTime startTime, DateTime? endTime = null);
    Task<ulong?> UpdateAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, DateTime startTime, DateTime? endTime = null);
}
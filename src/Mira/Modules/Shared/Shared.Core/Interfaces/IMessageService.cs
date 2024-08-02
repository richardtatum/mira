namespace Shared.Core.Interfaces;

public interface IMessageService
{
    Task<ulong?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, string? filePath = null);
    Task ModifyAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, string? filePath = null);
}
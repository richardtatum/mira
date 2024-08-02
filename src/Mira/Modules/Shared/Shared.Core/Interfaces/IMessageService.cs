namespace Shared.Core.Interfaces;

public interface IMessageService
{
    Task<ulong?> SendAsync(ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, byte[]? image = null);
    Task ModifyAsync(ulong messageId, ulong channelId, StreamStatus status, string url, int viewers, TimeSpan duration, string? playing = null, byte[]? image = null);
}
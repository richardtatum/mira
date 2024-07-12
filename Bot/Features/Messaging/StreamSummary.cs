using Mira.Features.Shared;

namespace Mira.Features.Messaging;

public class StreamSummary
{
    public string StreamKey { get; set; } = null!;
    public string Host { get; set; } = null!;
    public int ViewerCount { get; set; }
    public ulong ChannelId { get; set; }
    public StreamStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Url => $"{Host}/{StreamKey}";
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
}
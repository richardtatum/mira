using Mira.Features.Shared;

namespace Mira.Features.Polling.Models;

// TODO: Make this StreamSummary when we are done?
public class StreamOverview
{
    public int? Id { get; set; }
    public int SubscriptionId { get; set; }
    public string StreamKey { get; set; } = null!;
    public string HostUrl { get; set; } = null!;
    public StreamStatus Status { get; set; }
    public int ViewerCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ulong? MessageId { get; set; }
    public ulong ChannelId { get; set; }
    public string Url => $"{HostUrl}/{StreamKey}";
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow).Subtract(StartTime);

    public StreamRecord ToStreamRecord() => new()
    {
        Id = Id,
        SubscriptionId = SubscriptionId,
        Status = Status,
        ViewerCount = ViewerCount,
        MessageId = MessageId ?? throw new ArgumentNullException(),
        StartTime = StartTime,
        EndTime = EndTime
    };
}
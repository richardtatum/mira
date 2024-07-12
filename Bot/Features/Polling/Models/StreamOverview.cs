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
    public string Url => $"{HostUrl}/{StreamKey}";
    
    // Interfaced?
    public ulong? MessageId { get; set; }
    public ulong ChannelId { get; set; }

    public StreamRecord ToStreamRecord() => new StreamRecord
    {
        Id = Id,
        SubscriptionId = SubscriptionId,
        Status = Status,
        ViewerCount = ViewerCount,
        MessageId = MessageId ?? throw new ArgumentNullException(),
        StartTime = StartTime,
        EndTime = EndTime
    };
    
    
    // NotificationState property? New/Update
}
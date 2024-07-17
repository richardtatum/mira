using Mira.Features.Shared;

namespace Mira.Features.Polling.Models;

// TODO: Make this StreamSummary when we are done?
public class StreamOverview
{
    public string StreamKey { get; set; } = null!;
    public StreamStatus Status { get; set; }
    public ulong MessageId { get; set; }
    public ulong ChannelId { get; set; }
    public DateTime StartTime { get; set; }
}
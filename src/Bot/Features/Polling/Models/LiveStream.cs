namespace Mira.Features.Polling.Models;

public class LiveStream
{
    public int SubscriptionId { get; set; }
    public string StreamKey { get; set; }
    public string HostUrl { get; set; }
    public int ViewerCount { get; set; }
    public ulong ChannelId { get; set; }
    public DateTime StartTime { get; set; }
}
namespace Mira.Features.Polling.Models;

public class LiveStream
{
    public int SubscriptionId { get; set; }
    public string StreamKey { get; set; }
    public string Host { get; set; }
    public int ViewerCount { get; set; }
}
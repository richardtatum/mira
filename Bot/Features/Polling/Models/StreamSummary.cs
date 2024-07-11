namespace Mira.Features.Polling.Models;

public class StreamSummary
{
    public int SubscriptionId { get; set; }
    public string Host { get; set; } = null!;
    public string StreamKey { get; set; } = null!;
    public ulong Channel { get; set; }
    public int Viewers { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    public bool IsLive { get; set; }
    public string Url => $"{Host}/{StreamKey}";
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
}
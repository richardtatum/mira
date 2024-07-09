namespace Mira.Features.StreamChecker.Models;

public class StreamSummary
{
    public string Host { get; set; } = null!;
    public string StreamKey { get; set; } = null!;
    public ulong Channel { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Url => $"{Host}/{StreamKey}";
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
}
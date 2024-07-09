namespace Mira.Features.Shared.Models;

public class SubscriptionSummary
{
    public int? Id { get; set; }
    public string Host { get; set; } = null!;
    public string StreamKey { get; set; } = null!;
    public ulong Channel { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);

    public string Url => $"{Host}/{StreamKey}";
}
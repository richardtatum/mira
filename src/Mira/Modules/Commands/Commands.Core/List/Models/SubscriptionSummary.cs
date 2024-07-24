namespace Commands.Core.List.Models;

public class SubscriptionSummary
{
    public string Host { get; set; } = null!;
    public string? StreamKey { get; set; }
}
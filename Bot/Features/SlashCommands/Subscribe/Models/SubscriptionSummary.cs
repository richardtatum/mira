namespace Mira.Features.SlashCommands.Subscribe.Models;

public class SubscriptionSummary
{
    public int? Id { get; set; }
    public string Host { get; set; } = null!;
    public string StreamKey { get; set; } = null!;
    public string Url => $"{Host}/{StreamKey}";
}
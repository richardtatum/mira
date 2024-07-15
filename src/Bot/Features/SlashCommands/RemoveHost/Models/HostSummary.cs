namespace Mira.Features.SlashCommands.RemoveHost.Models;

public class HostSummary
{
    public int Id { get; set; }
    public string Url { get; set; }
    public int PollIntervalSeconds { get; set; }
    public int SubscriptionCount { get; set; }
}
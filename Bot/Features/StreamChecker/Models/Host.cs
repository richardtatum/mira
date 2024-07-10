namespace Mira.Features.StreamChecker.Models;

public class Host
{
    public string Url { get; set; } = null!;
    public int PollIntervalSeconds { get; set; }
}
namespace Mira.Features.StreamChecker.Models;

public class Host
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public int PollIntervalSeconds { get; set; }
    public ulong GuildId { get; set; }
}
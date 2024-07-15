namespace Mira.Features.Polling.Models;

public class Subscription
{
    public int Id { get; set; }
    public string StreamKey { get; set; } = null!;
    public ulong ChannelId { get; set; }
}
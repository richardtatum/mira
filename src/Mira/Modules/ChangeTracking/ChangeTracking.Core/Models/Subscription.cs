namespace ChangeTracking.Core.Models;

internal class Subscription
{
    public int Id { get; set; }
    public string StreamKey { get; set; } = null!;
    public ulong ChannelId { get; set; }
}
namespace Mira.Features.Polling.Models;

public class Subscription
{
    public int Id { get; set; }
    public string StreamKey { get; set; } = null!;
    public int HostId { get; set; }
    public ulong Channel { get; set; }
    public ulong CreatedBy { get; set; }
}
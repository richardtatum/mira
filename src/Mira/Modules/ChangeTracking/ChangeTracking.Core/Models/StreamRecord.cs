using Shared.Core;

namespace ChangeTracking.Core.Models;

internal class StreamRecord
{
    public int? Id { get; set; }
    public int SubscriptionId { get; set; }
    public StreamStatus Status { get; set; }
    public int ViewerCount { get; set; }
    public ulong MessageId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Playing { get; set; }
    public byte[]? Snapshot { get; set; }
}
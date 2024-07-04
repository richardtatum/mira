namespace Mira.Features.Shared.Models;

public class Notification : IEquatable<Notification>
{
    public int? Id { get; set; }
    public string StreamKey { get; set; } = null!;
    public ulong? HostId { get; set; }
    public ulong? Channel { get; set; }
    public ulong? CreatedBy { get; set; }

    public bool Equals(Notification? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && StreamKey == other.StreamKey && HostId == other.HostId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Notification)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, StreamKey, HostId);
    }
}
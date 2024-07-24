namespace Commands.Core.Subscribe.Models;

public class Subscription : IEquatable<Subscription>
{
    public int? Id { get; set; }
    public string StreamKey { get; set; } = null!;
    public int HostId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong CreatedBy { get; set; }

    public bool Equals(Subscription? other)
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
        return Equals((Subscription)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, StreamKey, HostId);
    }
}
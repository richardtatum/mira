namespace Mira.Features.ChangeTracking.Core.Models;

public class KeySummary
{
    public string StreamKey { get; set; }
    public long FirstSeenEpoch { get; set; }
    public VideoStream[] VideoStreams { get; set; } = [];
    public Viewer[] WhepSessions { get; set; } = [];
    public bool IsLive => VideoStreams.Any(stream => stream.SecondsSinceLastFrame < 15);
    public int ViewerCount => WhepSessions.Length;
    public DateTime StartTime => DateTimeOffset.FromUnixTimeSeconds(FirstSeenEpoch).DateTime;
}

public class VideoStream
{
    public string? Rid { get; set; }
    public long PacketsReceived { get; set; }
    public DateTime LastKeyFrameSeen { get; set; }
    public double SecondsSinceLastFrame => DateTime.UtcNow.Subtract(LastKeyFrameSeen).TotalSeconds;
}

public class Viewer
{
    public string? Id { get; set; }
}
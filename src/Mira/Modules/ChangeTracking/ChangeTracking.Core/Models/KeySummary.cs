namespace ChangeTracking.Core.Models;

internal class KeySummary
{
    public string StreamKey { get; set; } = null!;
    public long FirstSeenEpoch { get; set; }
    public VideoStream[] VideoStreams { get; set; } = [];
    public Viewer[] WhepSessions { get; set; } = [];
    public bool IsLive => VideoStreams.Any(stream => stream.IsLive);
    public int ViewerCount => WhepSessions.Length;
    public DateTime StartTime => DateTimeOffset.FromUnixTimeSeconds(FirstSeenEpoch).DateTime;
}

internal class VideoStream
{
    // The maximum time since the last frame before a stream is considered offline
    private const int MaxAllowedSecondsSinceLastFrame = 15;
    public DateTime LastKeyFrameSeen { get; set; }
    private double SecondsSinceLastFrame => DateTime.UtcNow.Subtract(LastKeyFrameSeen).TotalSeconds;
    public bool IsLive => SecondsSinceLastFrame < MaxAllowedSecondsSinceLastFrame;
}

internal class Viewer
{
    public string? Id { get; set; }
}
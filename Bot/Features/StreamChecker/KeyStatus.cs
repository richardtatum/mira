namespace Mira.Features.StreamChecker;

public class KeyStatus
{
    public string StreamKey { get; set; }
    public long FirstSeenEpoch { get; set; }
    public KeyStream[] VideoStreams { get; set; } = [];
    public bool IsLive => VideoStreams.Any(stream => stream.SecondsSinceLastFrame < 30);

}

public class KeyStream
{
    public string Rid { get; set; }
    public long PacketsReceived { get; set; }
    public DateTime LastKeyFrameSeen { get; set; }
    public double SecondsSinceLastFrame => DateTime.UtcNow.Subtract(LastKeyFrameSeen).TotalSeconds;
    
}
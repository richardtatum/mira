namespace Snapshot.Core;

public class SnapshotOptions
{
    public string? FfmpegLibPath { get; set; }
    public int QualityLevel { get; set; } = 75;
    public bool Enabled { get; set; }
}
namespace Snapshot.Core;

public class SnapshotOptions
{
    public bool Enabled { get; set; }
    public string? FfmpegLibPath { get; set; }
    public int QualityLevel { get; set; } = 75;
    public int ProcessFrequencySeconds { get; set; } = 30;
}
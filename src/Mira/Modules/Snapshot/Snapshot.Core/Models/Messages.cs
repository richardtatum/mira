namespace Snapshot.Core.Models;

public record FrameMessage(string StreamKey, IntPtr Ptr, int Width, int Height, int Stride);

public record ConvertedFrameMessage(string StreamKey, byte[] Frame);
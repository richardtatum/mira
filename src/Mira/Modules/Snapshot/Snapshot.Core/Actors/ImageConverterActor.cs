using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Snapshot.Core.Models;

namespace Snapshot.Core.Actors;

public class ImageConverterActor(
    ChannelWriter<ConvertedFrameMessage> next,
    ILogger<ImageConverterActor> logger
) : IActor<FrameMessage>
{
    public ChannelWriter<FrameMessage> Writer => _channel.Writer;
    private readonly Channel<FrameMessage> _channel = Channel.CreateUnbounded<FrameMessage>();
    private readonly ConcurrentDictionary<string, DateTime> _lastProcessed = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            if (_lastProcessed.TryGetValue(message.StreamKey, out var lastProcessed) &&
                now > lastProcessed.AddSeconds(30))
            {
                logger.LogInformation(
                    "[IMAGE-CONVERTER] Skipping frame for stream '{StreamKey}' as it was last processed at {lastProcessed}.",
                    message.StreamKey, lastProcessed);
                continue;
            }

            _lastProcessed[message.StreamKey] = now;
            logger.LogInformation("[IMAGE-CONVERTER] Converting frame for stream '${StreamKey}'.", message.StreamKey);
            var converted = await ConvertFrameToBytesAsync(message.Ptr, message.Width, message.Height, message.Stride);
            if (converted.Length <= 0)
            {
                logger.LogError("[IMAGE-CONVERTER] Failed to convert frame to bytes.");
                continue;
            }

            await next.WriteAsync(new ConvertedFrameMessage(message.StreamKey, converted), cancellationToken);
        }
    }

    private async Task<byte[]> ConvertFrameToBytesAsync(IntPtr ptr, int width, int height, int stride)
    {
        try
        {
            const int bytesPerPixel = 3; // Assuming RGB format
            var totalBytes = height * stride; // Calculate total bytes in the image

            // Verify that totalBytes is not negative or unreasonably large
            if (totalBytes <= 0)
            {
                logger.LogError("[IMAGE-CONVERTER] Invalid image dimensions or stride.");
                return [];
            }

            var pixelData = new byte[totalBytes];

            // Use try-catch to handle potential access violations
            try
            {
                Marshal.Copy(ptr, pixelData, 0, totalBytes);
            }
            catch (AccessViolationException ex)
            {
                logger.LogError(ex, "Access violation while copying memory.");
                return [];
            }

            // The data is in BGR format it needs to be converted to RGB
            SwapRedBlueChannels(width, height, stride, bytesPerPixel, pixelData);

            // Create ImageSharp image from pixel data
            using var image = Image.LoadPixelData<Rgb24>(pixelData, width, height);

            using var stream = new MemoryStream();
            await image.SaveAsWebpAsync(stream, new WebpEncoder
            {
                Quality = 50
            });

            return stream.ToArray();
        }
        catch (OutOfMemoryException ex)
        {
            logger.LogError(ex, "[IMAGE-CONVERTER] Out of memory error while processing image.");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IMAGE-CONVERTER] Unexpected error while extracting frame.");
            return [];
        }
    }

    private void SwapRedBlueChannels(int width, int height, int stride, int bytesPerPixel, byte[] pixelData)
    {
        // Calculate the expected length based on input parameters
        var expectedLength = height * stride;
        if (pixelData.Length != expectedLength)
        {
            logger.LogWarning("[IMAGE-CONVERTER] Pixel data length mismatch. Expected: {Expected}, Actual: {Actual}",
                expectedLength, pixelData.Length);
            // Adjust height or width if necessary
            height = Math.Min(height, pixelData.Length / stride);
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * stride + x * bytesPerPixel;
                if (index + 2 >= pixelData.Length) continue;

                // Swap BGR to RGB
                (pixelData[index], pixelData[index + 2]) = (pixelData[index + 2], pixelData[index]);
            }
        }
    }
}
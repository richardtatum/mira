using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Snapshot.Core;

public class ImageProcessor(ILogger<ImageProcessor> logger)
{
    public async Task<byte[]> ExtractFrameAsync(IntPtr ptr, int width, int height, int stride, CancellationToken cancellationToken)
    {
        logger.LogInformation("[IMAGE-PROCESSOR] Extracting frame.");
        try
        {
            var bytesPerPixel = 3; // Assuming RGB format
            var totalBytes = height * stride; // Calculate total bytes in the image

            // Verify that totalBytes is not negative or unreasonably large
            if (totalBytes <= 0)
            {
                throw new ArgumentException("Invalid image dimensions or stride.");
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
                throw;
            }

            // The data is in BGR format it needs to be converted to RGB
            SwapRedBlueChannels(width, height, stride, bytesPerPixel, pixelData);

            // Create ImageSharp image from pixel data
            using var image = Image.LoadPixelData<Rgb24>(pixelData, width, height);

            using var stream = new MemoryStream();
            await image.SaveAsWebpAsync(stream, new WebpEncoder
            {
                Quality = 50
            }, cancellationToken);

            return stream.ToArray();
        }
        catch (OutOfMemoryException ex)
        {
            logger.LogError(ex, "[IMAGE-PROCESSOR] Out of memory error while processing image.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[IMAGE-PROCESSOR] Unexpected error while extracting frame.");
            throw;
        }
    }

    private void SwapRedBlueChannels(int width, int height, int stride, int bytesPerPixel, byte[] pixelData)
    {
        // Calculate the expected length based on input parameters
        var expectedLength = height * stride;
        if (pixelData.Length != expectedLength)
        {
            logger.LogWarning("[IMAGE-PROCESSOR] Pixel data length mismatch. Expected: {Expected}, Actual: {Actual}", expectedLength, pixelData.Length);
            // Adjust height or width if necessary
            height = Math.Min(height, pixelData.Length / stride);
        }
        
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = y * stride + x * bytesPerPixel;
                if (index + 2 >= pixelData.Length)
                {
                    logger.LogInformation("[IMAGE-PROCESSOR] Skipping y:x  {y}:{x}", y, x);
                    continue;
                }

                // Swap BGR to RGB
                (pixelData[index], pixelData[index + 2]) = (pixelData[index + 2], pixelData[index]);
            }
        }
    }
}
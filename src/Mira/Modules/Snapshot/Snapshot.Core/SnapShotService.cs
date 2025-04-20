using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Interfaces;
using Snapshot.Core.Actors;
using Snapshot.Core.Models;

namespace Snapshot.Core;

public class SnapShotService(
    IOptions<SnapshotOptions> snapshotOptions,
    ILogger<SnapShotService> logger,
    QueryRepository queryRepository,
    IWhepConnectionFactory whepConnectionFactory,
    IActor<FrameMessage> imageConverterActor)
    : ISnapshotService
{

    public async Task ExecuteAsync(string hostUrl, CancellationToken cancellationToken = default)
    {
        var currentOptions = snapshotOptions.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("[SNAPSHOT] Snapshots disabled. Skipping.");
            return;
        }

        var streamKeys = await queryRepository.GetLiveStreamKeysAsync(hostUrl, cancellationToken);
        logger.LogDebug("[SNAPSHOT][{Host}] {Count} live stream(s) found. Attempting to obtain snapshots.",
            hostUrl, streamKeys.Length);

        // Create a new cancellation token with the parent one as the source. Set a timeout to prevent 'ExecuteAsync' from
        // running forever
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var frameTimeout = TimeSpan.FromSeconds(currentOptions.FrameTimeoutSeconds);
        cts.CancelAfter(frameTimeout);

        var token = cts.Token;
        var tasks = streamKeys.Select(key => ExecuteAsync(hostUrl, key, token));
        
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[SNAPSHOT][{Host}] Timed out attempting to obtain snapshots.", hostUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SNAPSHOT][{Host}] Unexpected error during snapshot collection.", hostUrl);
        }
    }

    public async Task ExecuteAsync(string hostUrl, string streamKey, CancellationToken cancellationToken = default)
    {
        var frameWritten = false;

        logger.LogDebug("[SNAPSHOT][{Host}][{Key}] Attempting snapshot.", hostUrl, streamKey);
        using var connection = await whepConnectionFactory.CreateAsync(hostUrl, streamKey, cancellationToken);
        if (connection is null)
        {
            logger.LogWarning("[SNAPSHOT][{Host}][{Key}] Failed to create connection.", hostUrl, streamKey);
            return;
        }

        connection.FrameReceived += async image =>
        {
            logger.LogDebug("[SNAPSHOT][{Host}][{Key}] New frame received.", hostUrl, streamKey);
            var message = new FrameMessage(streamKey, image.Sample, image.Width, image.Height, image.Stride);
            await imageConverterActor.Writer.WriteAsync(message, cancellationToken);
            frameWritten = true;
        };

        connection.Disposed += () =>
        {
            logger.LogDebug("[SNAPSHOT][{Host}][{Key}] Connection closed.", hostUrl, streamKey);
        };

        await connection.StartAsync();
        logger.LogDebug("[SNAPSHOT][{Host}][{Key}] Connection open.", hostUrl, streamKey);

        while (!frameWritten && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
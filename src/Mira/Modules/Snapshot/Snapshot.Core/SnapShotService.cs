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
    private readonly SnapshotOptions _options = snapshotOptions.Value;

    public async Task ExecuteAsync(string hostUrl, CancellationToken cancellationToken = default)
    {
        var currentOptions = snapshotOptions.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("[SNAPSHOT] Snapshots disabled. Skipping.");
            return;
        }

        var streamKeys = await queryRepository.GetLiveStreamKeysAsync(hostUrl, cancellationToken);
        logger.LogInformation("[SNAPSHOT][{Host}] {Count} live stream(s) found. Attempting to obtain snapshots.",
            hostUrl, streamKeys.Length);
        await Task.WhenAll(streamKeys.Select(key => ExecuteAsync(hostUrl, key, cancellationToken)));
    }

    public async Task ExecuteAsync(string hostUrl, string streamKey, CancellationToken cancellationToken = default)
    {
        var frameWritten = false;

        logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Attempting snapshot.", hostUrl, streamKey);
        using var connection = await whepConnectionFactory.CreateAsync(hostUrl, streamKey, cancellationToken);
        if (connection is null)
        {
            logger.LogError("[SNAPSHOT][{Host}][{Key}] Failed to create connection.", hostUrl, streamKey);
            return;
        }

        connection.FrameReceived += async image =>
        {
            logger.LogInformation("[SNAPSHOT][{Host}][{Key}] New frame received.", hostUrl, streamKey);
            var message = new FrameMessage(streamKey, image.Sample, image.Width, image.Height, image.Stride);
            await imageConverterActor.Writer.WriteAsync(message, cancellationToken);
            frameWritten = true;
        };

        connection.Disposed += () =>
        {
            logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection closed.", hostUrl, streamKey);
        };

        await connection.StartAsync();
        logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection open.", hostUrl, streamKey);

        while (!frameWritten && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
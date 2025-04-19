using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Interfaces;
using Snapshot.Core.Actors;
using Snapshot.Core.Models;

namespace Snapshot.Core;

public class SnapShotService : ISnapshotService
{
    private readonly ILogger<SnapShotService> _logger;

    private readonly SnapshotOptions _options;
    private readonly QueryRepository _queryRepository;
    private readonly IWhepConnectionFactory _whepConnectionFactory;
    private readonly IActor<FrameMessage> _imageConverterActor;

    private List<string> _completedSnapshots = new();

    public SnapShotService(IOptions<SnapshotOptions> snapshotOptions, ILogger<SnapShotService> logger,
        QueryRepository queryRepository, IWhepConnectionFactory whepConnectionFactory,
        IActor<FrameMessage> imageConverterActor)
    {
        _options = snapshotOptions.Value;
        _logger = logger;
        _queryRepository = queryRepository;
        _whepConnectionFactory = whepConnectionFactory;
        _imageConverterActor = imageConverterActor;
    }

    public async Task ExecuteAsync(string hostUrl, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("[SNAPSHOT] Snapshots disabled. Skipping.");
            return;
        }

        // TODO: I want this to be a list of live keys, with a subscribe and unsubscribe method for retrieving snapshots.
        // the event that fires when it is live should then be accessible to set.
        // TODO: Prevent the service from checking for snapshot if the stream is actually down. (db online, now offline)
        // TODO: Keep the stream open until reported offline?

        var streamKeys = await _queryRepository.GetLiveStreamKeysAsync(hostUrl, cancellationToken);
        _logger.LogInformation("[SNAPSHOT][{Host}] {Count} live stream(s) found. Attempting to obtain snapshots.",
            hostUrl, streamKeys.Length);
        await Task.WhenAll(streamKeys.Select(key => ExecuteAsync(hostUrl, key, cancellationToken)));
    }

    public async Task ExecuteAsync(string hostUrl, string streamKey, CancellationToken cancellationToken = default)
    {
        var frameWritten = false;

        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Attempting snapshot.", hostUrl, streamKey);
        using var connection = await _whepConnectionFactory.CreateAsync(hostUrl, streamKey, cancellationToken);
        if (connection is null)
        {
            _logger.LogError("[SNAPSHOT][{Host}][{Key}] Failed to create connection.", hostUrl, streamKey);
            return;
        }

        connection.FrameReceived += async image =>
        {
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] New frame received.", hostUrl, streamKey);
            var message = new FrameMessage(streamKey, image.Sample, image.Width, image.Height, image.Stride);
            await _imageConverterActor.Writer.WriteAsync(message, cancellationToken);
            frameWritten = true;
        };

        connection.Disposed += () =>
        {
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection closed.", hostUrl, streamKey);
        };

        await connection.StartAsync();
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection open.", hostUrl, streamKey);

        while (!frameWritten && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
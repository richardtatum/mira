using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Core.Interfaces;

namespace Snapshot.Core;

public class SnapShotService : ISnapshotService
{
    private readonly ILogger<SnapShotService> _logger;
    private readonly CommandRepository _commandRepository;
    private readonly SnapshotOptions _options;
    private readonly QueryRepository _queryRepository;
    private readonly IWhepConnectionFactory _whepConnectionFactory;
    private readonly ImageProcessor _imageProcessor;
    private List<string> _completedSnapshots = new();
    
    public SnapShotService(IOptions<SnapshotOptions> snapshotOptions, ILogger<SnapShotService> logger, CommandRepository commandRepository, QueryRepository queryRepository, IWhepConnectionFactory whepConnectionFactory, ImageProcessor imageProcessor)
    {
        _options = snapshotOptions.Value;
        _logger = logger;
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _whepConnectionFactory = whepConnectionFactory;
        _imageProcessor = imageProcessor;
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
        _logger.LogInformation("[SNAPSHOT][{Host}] {Count} live stream(s) found. Attempting to obtain snapshots.", hostUrl, streamKeys.Length);
        await Task.WhenAll(streamKeys.Select(key => ExecuteAsync(hostUrl, key, cancellationToken)));
    }

    public async Task ExecuteAsync(string hostUrl, string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Attempting snapshot.", hostUrl, streamKey);
        using var connection = await _whepConnectionFactory.CreateAsync(hostUrl, streamKey, cancellationToken);
        if (connection is null)
        {
            _logger.LogError("[SNAPSHOT][{Host}][{Key}] Failed to create connection.", hostUrl, streamKey);
            return;
        }

        connection.FrameReceived += async image =>
        {
            if (_completedSnapshots.Contains(streamKey))
            {
                return;
            }

            await SaveFrameToDatabaseAsync(streamKey, image.Sample, image.Width, image.Height, image.Stride,
                cancellationToken);
            _completedSnapshots.Add(streamKey);
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Snapshot saved successfully", hostUrl, streamKey);
        };

        connection.Disposed += () =>
        {
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection closed.", hostUrl, streamKey);
        };
        
        await connection.StartAsync();
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Connection open.", hostUrl, streamKey);

        // Need to add a maximum timeout
        var attempts = 0;
        _completedSnapshots = []; // This doesn't work, it would wipe all the other in progress snapshots
        while (!_completedSnapshots.Contains(streamKey) && attempts < 10)
        {
            attempts++;
            _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Awaiting snapshot completion. Attempt: {Attempt}", hostUrl, streamKey, attempts);
            await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
        }
        
        _logger.LogInformation("[SNAPSHOT][{Host}][{Key}] Completed.", hostUrl, streamKey);
    }

    private async Task SaveFrameToDatabaseAsync(string streamKey, IntPtr ptr, int width, int height, int stride,
        CancellationToken cancellationToken)
    {
        var bytes = await _imageProcessor.ExtractFrameAsync(ptr, width, height, stride, cancellationToken);
        _logger.LogInformation("[SNAPSHOT] Frame extracted");
        await _commandRepository.SaveSnapshotAsync(streamKey, bytes, cancellationToken);
        _logger.LogInformation("[SNAPSHOT] Frame saved");
    }
}
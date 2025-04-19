using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Snapshot.Core.Models;

namespace Snapshot.Core.Actors;

public class DatabaseWriterActor(
    CommandRepository commandRepository,
    ILogger<DatabaseWriterActor> logger
) : IActor<ConvertedFrameMessage>
{
    public ChannelWriter<ConvertedFrameMessage> Writer => _channel.Writer;
    private readonly Channel<ConvertedFrameMessage> _channel = Channel.CreateUnbounded<ConvertedFrameMessage>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            await commandRepository.SaveSnapshotAsync(message.StreamKey, message.Frame, cancellationToken);
            logger.LogInformation("[DATABASE-WRITER] Snapshot saved for ${StreamKey}", message.StreamKey);
        }
    }
}
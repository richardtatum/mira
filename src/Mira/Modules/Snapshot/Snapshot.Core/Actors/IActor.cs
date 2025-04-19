using System.Threading.Channels;

namespace Snapshot.Core.Actors;

public interface IActor<T>
{
    ChannelWriter<T> Writer { get; }
    Task StartAsync(CancellationToken cancellationToken);
}
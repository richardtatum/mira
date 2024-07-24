using Discord.WebSocket;

namespace Cleanup.Core;

public interface ICleanupService<T> where T : SocketEntity<ulong>
{
    Task ExecuteAsync(T socketEntity);
}
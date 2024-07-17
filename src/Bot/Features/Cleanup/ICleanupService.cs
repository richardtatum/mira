using Discord.WebSocket;

namespace Mira.Features.Cleanup;

public interface ICleanupService<T> where T : SocketEntity<ulong>
{
    Task ExecuteAsync(T socketEntity);
}
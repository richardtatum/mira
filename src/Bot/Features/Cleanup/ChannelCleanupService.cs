using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Mira.Features.Cleanup;

public class ChannelCleanupService(CommandRepository commandRepository, ILogger<ChannelCleanupService> logger) : ICleanupService<SocketChannel>
{
    public Task ExecuteAsync(SocketChannel channel)
    {
        logger.LogInformation("[CLEANUP][{ChannelId} Channel destroyed event fired. Removing any associated subscriptions.", channel.Id);
        return commandRepository.CleanupChannelAsync(channel.Id);
    }
}
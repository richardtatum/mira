using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Mira.Features.Cleanup;

public class GuildCleanupService(CommandRepository commandRepository, ILogger<GuildCleanupService> logger) : ICleanupService<SocketGuild>
{
    public Task ExecuteAsync(SocketGuild guild)
    {
        logger.LogInformation("[CLEANUP][{GuildName}] Bot removed from guild. Removing any associated hosts and subscriptions.", guild.Name);
        return commandRepository.CleanupChannelAsync(guild.Id);
    }
}
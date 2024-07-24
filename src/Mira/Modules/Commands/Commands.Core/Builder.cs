using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Commands.Core;

public class Builder(DiscordSocketClient client, IEnumerable<ISlashCommand> commands, ILogger<Builder> logger)
{
    public async Task OnReadyAsync()
    {
        try
        {
            var commandProperties = commands
                .Select(command => command.BuildCommand())
                .ToArray();

            await client.BulkOverwriteGlobalApplicationCommandsAsync(commandProperties);
        }
        catch (HttpException ex)
        {
            logger.LogError("[COMMAND-BUILDER] Failed to build commands. Ex: {Exception} ", ex);
        }

    }
}
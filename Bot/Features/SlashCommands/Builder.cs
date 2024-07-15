using System.Text.Json;
using Discord.Net;
using Discord.WebSocket;

namespace Mira.Features.SlashCommands;

public class Builder
{
    private readonly DiscordSocketClient _client;
    private readonly IEnumerable<ISlashCommand> _commands;

    public Builder(DiscordSocketClient client, IEnumerable<ISlashCommand> commands)
    {
        _client = client;
        _commands = commands;
    }

    public async Task OnReadyAsync()
    {
        try
        {
            var commandProperties = _commands
                .Select(command => command.BuildCommand())
                .ToArray();

            await _client.BulkOverwriteGlobalApplicationCommandsAsync(commandProperties);
        }
        catch (HttpException ex)
        {
            // TODO: Update to use logger
            var json = JsonSerializer.Serialize(ex.Errors, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Console.WriteLine(json);
        }

    }
}
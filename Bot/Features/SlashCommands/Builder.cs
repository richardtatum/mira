using System.Text.Json;
using Discord.Net;
using Discord.WebSocket;
using Mira.Interfaces;

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
            foreach (var command in _commands)
            {
                var commandProperties= await command.BuildCommandAsync();
                await _client.CreateGlobalApplicationCommandAsync(commandProperties);
            }
        }
        catch (HttpException ex)
        {
            var json = JsonSerializer.Serialize(ex.Errors, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Console.WriteLine(json);
        }

    }
}
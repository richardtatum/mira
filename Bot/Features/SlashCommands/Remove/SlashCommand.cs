using Discord;
using Discord.WebSocket;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands.Remove;

public class SlashCommand
{
    public string Name => "remove";
    public Task<SlashCommandProperties> BuildCommandAsync()
    {
        throw new NotImplementedException();
    }

    public Task RespondAsync(SocketSlashCommand command)
    {
        throw new NotImplementedException();
    }
}
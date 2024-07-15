using Discord;
using Discord.WebSocket;

namespace Mira.Features.SlashCommands;

public interface ISlashCommand
{
    string Name { get; }
    ApplicationCommandProperties BuildCommand();
    Task RespondAsync(SocketSlashCommand command);
}
using Discord;
using Discord.WebSocket;

namespace Commands.Core;

public interface ISlashCommand
{
    string Name { get; }
    ApplicationCommandProperties BuildCommand();
    Task RespondAsync(SocketSlashCommand command);
}
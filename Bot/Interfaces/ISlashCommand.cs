using Discord;
using Discord.WebSocket;

namespace Mira.Interfaces;

public interface ISlashCommand
{
    string Name { get; }
    ApplicationCommandProperties BuildCommand();
    Task RespondAsync(SocketSlashCommand command);
}
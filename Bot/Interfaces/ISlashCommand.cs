using Discord;
using Discord.WebSocket;

namespace Mira.Interfaces;

public interface ISlashCommand
{
    string Name { get; }
    Task<SlashCommandProperties> BuildCommandAsync();
    Task RespondAsync(SocketSlashCommand command);
}
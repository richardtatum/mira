using Discord.WebSocket;

namespace Mira.Features.SlashCommands;

public interface ISelectable
{
    bool HandlesComponent(SocketMessageComponent component);
    Task RespondAsync(SocketMessageComponent component);
}
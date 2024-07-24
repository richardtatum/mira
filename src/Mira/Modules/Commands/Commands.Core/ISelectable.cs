using Discord.WebSocket;

namespace Commands.Core;

public interface ISelectable
{
    bool HandlesComponent(SocketMessageComponent component);
    Task RespondAsync(SocketMessageComponent component);
}
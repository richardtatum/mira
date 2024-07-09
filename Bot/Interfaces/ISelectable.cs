using Discord.WebSocket;

namespace Mira.Interfaces;

public interface ISelectable
{
    bool HandlesComponent(SocketMessageComponent component);
    Task RespondAsync(SocketMessageComponent component);
}
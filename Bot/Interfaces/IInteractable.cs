using Discord.WebSocket;

namespace Mira.Interfaces;

public interface IInteractable
{
    Task RespondAsync(SocketInteraction interaction);
}
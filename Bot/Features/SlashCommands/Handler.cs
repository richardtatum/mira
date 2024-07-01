using Discord.WebSocket;
using Mira.Interfaces;

namespace Mira.Features.SlashCommands;

public class Handler
{
    private readonly IEnumerable<ISlashCommand> _commands;
    private readonly IEnumerable<IInteractable> _interactables;

    public Handler(IEnumerable<ISlashCommand> commands, IEnumerable<IInteractable> interactables)
    {
        _commands = commands;
        _interactables = interactables;
    }

    public Task HandleCommandExecutedAsync(SocketSlashCommand command) =>
        _commands
            .Where(x => x.Name == command.Data.Name)
            .Select(x => x.RespondAsync(command))
            .FirstOrDefault() ?? Task.CompletedTask;

    public Task HandleInteractionCreatedAsync(SocketInteraction interaction)
    {
        return _interactables
            .Select(x => x.RespondAsync(interaction))
            .FirstOrDefault() ?? Task.CompletedTask;
    }
}
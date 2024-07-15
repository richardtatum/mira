using Discord.WebSocket;

namespace Mira.Features.SlashCommands;

public class Handler
{
    private readonly IEnumerable<ISlashCommand> _commands;
    private readonly IEnumerable<ISelectable> _selectables;

    public Handler(IEnumerable<ISlashCommand> commands, IEnumerable<ISelectable> selectables)
    {
        _commands = commands;
        _selectables = selectables;
    }

    public Task HandleCommandExecutedAsync(SocketSlashCommand command) =>
        _commands
            .Where(x => x.Name == command.Data.Name)
            .Select(x => x.RespondAsync(command))
            .FirstOrDefault() ?? Task.CompletedTask;

    public Task HandleSelectMenuExecutedAsync(SocketMessageComponent component) =>
        _selectables
            .Where(x => x.HandlesComponent(component))
            .Select(x => x.RespondAsync(component))
            .FirstOrDefault() ?? Task.CompletedTask;
}
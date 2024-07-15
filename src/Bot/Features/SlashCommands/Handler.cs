using Discord.WebSocket;

namespace Mira.Features.SlashCommands;

public class Handler(IEnumerable<ISlashCommand> commands, IEnumerable<ISelectable> selectables)
{
    public Task HandleCommandExecutedAsync(SocketSlashCommand command) =>
        commands
            .Where(x => x.Name == command.Data.Name)
            .Select(x => x.RespondAsync(command))
            .FirstOrDefault() ?? Task.CompletedTask;

    public Task HandleSelectMenuExecutedAsync(SocketMessageComponent component) =>
        selectables
            .Where(x => x.HandlesComponent(component))
            .Select(x => x.RespondAsync(component))
            .FirstOrDefault() ?? Task.CompletedTask;
}
using Discord.WebSocket;

namespace Mira.Features.SlashCommands;

public static class ReadOnlyCollectionExtensions
{
    public static T? GetValue<T>(this IReadOnlyCollection<SocketSlashCommandDataOption> collection, string name)
    {
        return (T?)collection.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))?.Value;
    }
}
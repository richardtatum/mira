namespace Mira.Features.SlashCommands.Notify;

public class Host
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public ulong? GuildId { get; set; }
}
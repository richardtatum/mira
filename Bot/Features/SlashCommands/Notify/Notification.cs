namespace Mira.Features.SlashCommands.Notify;

public class Notification
{
    public int? Id { get; set; }
    public string StreamKey { get; set; } = null!;
    public ulong? HostId { get; set; }
    public ulong? Channel { get; set; }
    public ulong? CreatedBy { get; set; }
}
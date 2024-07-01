namespace Mira.Features.SlashCommands.Notify;

public class Notification(string token, string? id = null,  string? host = null, string? mention = null)
{
    public string? Id { get; set; } = id;
    public string Token { get; set; } = token;
    public string? Host { get; set; } = host;
    public string? Mention { get; set; } = mention;

    public string Url => string.IsNullOrWhiteSpace(Host) ? "" : $"{Host}/{Token}";
}
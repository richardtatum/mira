namespace Mira.Features.SlashCommands.AddHost.Models;

public record Host(string Url, int PollIntervalSeconds, ulong GuildId, ulong CreatedBy);
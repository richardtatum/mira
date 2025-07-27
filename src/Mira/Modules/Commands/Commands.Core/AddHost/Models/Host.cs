namespace Commands.Core.AddHost.Models;

public record Host(string Url, int PollIntervalSeconds, ulong GuildId, ulong CreatedBy, string? AuthHeader = null);
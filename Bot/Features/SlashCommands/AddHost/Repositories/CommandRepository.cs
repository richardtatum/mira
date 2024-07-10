using Dapper;
using Mira.Data;

namespace Mira.Features.SlashCommands.AddHost.Repositories;

public class CommandRepository(DbContext context)
{
    internal async Task AddHostAsync(string url, double pollInterval, ulong guildId)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO host (url, poll_interval_seconds, guild_id) VALUES (@url, @pollInterval, @guildId)", new
            {
                url,
                pollInterval,
                guildId
            });
    }
}
using Commands.Core.AddHost.Models;
using Dapper;
using Shared.Core;

namespace Commands.Core.AddHost.Repositories;

public class CommandRepository(DbContext context)
{
    internal async Task AddHostAsync(Host host)
    {
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO host
                (url, poll_interval_seconds, guild_id, created_by)
                VALUES (@url, @pollIntervalSeconds, @guildId, @createdBy)", new
            {
                url = host.Url,
                pollIntervalSeconds = host.PollIntervalSeconds,
                guildId = host.GuildId,
                createdBy = host.CreatedBy
            });
    }
}
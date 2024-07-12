using Dapper;
using Mira.Data;
using Mira.Features.SlashCommands.Subscribe.Models;

namespace Mira.Features.SlashCommands.Subscribe.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<Host[]> GetHostsAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Host>(
            "SELECT id, url FROM host WHERE guild_id = @guildId",
        new {
            guildId,
        });

        return results.ToArray();
    }

    internal async Task<Host> GetHostAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Host>(
            "SELECT id, url FROM host WHERE id = @id", new
            {
                id
            });
    }

    internal async Task<Subscription?> GetSubscriptionAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Subscription>(
            "SELECT id, stream_key streamKey, channel_id channelId, created_by createdBy FROM subscription WHERE id = @id", new
            {
                id
            });
    }
}
using Dapper;
using Mira.Data;

namespace Mira.Features.SlashCommands.Notify.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<Host[]> GetHostsAsync(ulong? guildId)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Host>(
            "SELECT id, url, guild_id FROM host WHERE guild_id = @guildId OR guild_id = @global",
        new {
            guildId,
            global = -1
        });

        return results.ToArray();
    }

    internal async Task<Host> GetHostAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Host>(
            "SELECT id, url, guild_id guildId FROM host WHERE id = @id", new
            {
                id
            });
    }

    internal async Task<Notification?> GetNotificationAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Notification>(
            "SELECT id, token, channel, created_by createdBy FROM notification WHERE id = @id", new
            {
                id
            });
    }
}
using Commands.Core.Subscribe.Models;
using Dapper;
using Shared.Core;

namespace Commands.Core.Subscribe.Repositories;

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

    internal async Task<Host?> GetHostAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Host?>(
            "SELECT id, url FROM host WHERE id = @id", new
            {
                id
            });
    }

    internal async Task<bool> HostStreamKeyExistsAsync(int hostId, string streamKey)
    {
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM subscription WHERE host_id = @hostId AND stream_key = @streamKey", new
            {
                hostId,
                streamKey
            });

        return result > 0;
    }
}
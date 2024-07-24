using Commands.Core.RemoveHost.Models;
using Dapper;
using Shared.Core;

namespace Commands.Core.RemoveHost.Repositories;

public class QueryRepository(DbContext context)
{
    public async Task<HostSummary[]> GetHostsAsync(ulong guildId)
    {
        using var connection = context.CreateConnection();
        var result = await connection.QueryAsync<HostSummary>(
            @"SELECT h.id, h.url, h.poll_interval_seconds pollIntervalSeconds, COUNT(s.id) subscriptionCount
                FROM host h 
                LEFT JOIN subscription s ON s.host_id = h.id
                WHERE h.guild_id = @guildId", new
            {
                guildId
            });

        return result.ToArray();
    }

    public async Task<Host?> GetHostAsync(int id)
    {
        using var connection = context.CreateConnection();
        return await connection.QueryFirstAsync<Host?>(
            "SELECT id, url, created_by createdBy FROM host WHERE id = @id LIMIT 1", new
            {
                id
            });
    }
    
}
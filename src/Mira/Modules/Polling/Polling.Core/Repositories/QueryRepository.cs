using Dapper;
using Shared.Core;
using Shared.Core.Models;

namespace Polling.Core.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<Host[]> GetHostsAsync()
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Host>(
            @"SELECT h.url, MIN(h.poll_interval_seconds) pollIntervalSeconds, h.auth_header authHeader
                FROM host h
                INNER JOIN subscription s ON h.id = s.host_id
                GROUP BY h.url"
        );

        return results.ToArray();
    }
}

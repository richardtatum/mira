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
            @"SELECT url, MIN(poll_interval_seconds) pollIntervalSeconds, auth_header authHeader
                FROM host
                GROUP BY url"
        );

        return results.ToArray();
    }
}
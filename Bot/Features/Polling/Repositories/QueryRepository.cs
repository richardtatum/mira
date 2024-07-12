using Dapper;
using Mira.Data;
using Mira.Features.Polling.Models;
using Mira.Features.Shared;

namespace Mira.Features.Polling.Repositories;

public class QueryRepository(DbContext context)
{
    internal async Task<StreamRecord[]> GetLiveStreamsAsync(IEnumerable<int> subscriptionIds)
    {
        using var connection = context.CreateConnection();

        var results = await connection.QueryAsync<StreamRecord>(
            @"SELECT id, subscription_id subscriptionId, status, start_time startTime, end_time endTime
                FROM stream
                WHERE subscription_id IN @subscriptionIds AND status = @live", new
            {
                subscriptionIds,
                live = StreamStatus.Live
            });

        return results.ToArray();
    }



    internal async Task<Host[]> GetHostsAsync()
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Host>(
            @"SELECT url, MIN(poll_interval_seconds) pollIntervalSeconds
                FROM host
                GROUP BY url"
        );

        return results.ToArray();
    }

    internal async Task<Subscription[]> GetSubscriptionsAsync(string hostUrl)
    {
        using var connection = context.CreateConnection();
        var results = await connection.QueryAsync<Subscription>(
            @"SELECT s.id, s.stream_key streamKey, s.channel_id channelId
                FROM subscription s
                INNER JOIN host h ON s.host_id = h.id
                WHERE h.url = @hostUrl", new
        {
            hostUrl
        });

        return results.ToArray();
    }
}
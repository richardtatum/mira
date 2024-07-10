using Dapper;
using Mira.Data;
using Mira.Features.StreamChecker.Models;

namespace Mira.Features.StreamChecker.Repositories;

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

    internal async Task<StreamSummary> GetStreamSummaryAsync(int subscriptionId)
    {
        using var connection = context.CreateConnection();

        return await connection.QueryFirstAsync<StreamSummary>(
            @"SELECT h.url host, n.stream_key streamKey, n.channel, s.start_time startTime, s.end_time endTime
                FROM subscription n
                INNER JOIN host h ON h.id = n.host_id
                INNER JOIN stream s ON s.subscription_id = n.id
                WHERE n.id = @subscriptionId", new
            {
                subscriptionId
            });
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
            @"SELECT s.id, s.stream_key streamKey, s.channel, s.created_by createdBy 
                FROM subscription s
                INNER JOIN host h ON s.host_id = h.id
                WHERE h.url = @hostUrl", new
        {
            hostUrl
        });

        return results.ToArray();
    }
}